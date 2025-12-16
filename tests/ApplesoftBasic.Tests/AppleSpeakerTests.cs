// <copyright file="AppleSpeakerTests.cs" company="Josh Pactor">
// Copyright (c) Josh Pactor. All rights reserved.
// </copyright>

namespace ApplesoftBasic.Tests;

using System.Reflection;

using Interpreter.Emulation;

using Microsoft.Extensions.Logging;

using Moq;

/// <summary>
/// Contains unit tests for verifying the behavior and functionality of the AppleSpeaker class,
/// including speaker click generation, beep tone generation, buffer flushing, thread safety,
/// and proper resource disposal.
/// </summary>
[TestFixture]
public class AppleSpeakerTests
{
    private Mock<ILogger<AppleSpeaker>> mockLogger = null!;
    private AppleSpeaker speaker = null!;
    private TestAudioOutput testAudioOutput = null!;

    /// <summary>
    /// Sets up the test environment by initializing the speaker emulation with a test audio output.
    /// </summary>
    [SetUp]
    public void Setup()
    {
        mockLogger = new Mock<ILogger<AppleSpeaker>>();
        testAudioOutput = new TestAudioOutput();
        speaker = new AppleSpeaker(mockLogger.Object);

        // Use reflection to inject the test audio output
        var audioOutputField = typeof(AppleSpeaker).GetField("audioOutput", BindingFlags.NonPublic | BindingFlags.Instance);
        audioOutputField?.SetValue(speaker, testAudioOutput);
    }

    /// <summary>
    /// Cleans up test resources by disposing the speaker instance.
    /// </summary>
    [TearDown]
    public void TearDown()
    {
        speaker?.Dispose();
        testAudioOutput?.Dispose();
    }

    /// <summary>
    /// Verifies that the Click() method generates audio samples in the buffer.
    /// </summary>
    [Test]
    public void Click_GeneratesAudioSamples()
    {
        // Act
        speaker.Click();
        speaker.Flush();

        // Assert - Verify that audio samples were played
        Assert.That(testAudioOutput.PlayCallCount, Is.GreaterThan(0), "Click should generate audio samples");
        Assert.That(testAudioOutput.LastSamples, Is.Not.Null, "Audio samples should not be null");
        Assert.That(testAudioOutput.LastSamples!.Length, Is.GreaterThan(0), "Audio samples should not be empty");
    }

    /// <summary>
    /// Verifies that multiple Click() calls toggle speaker state and generate different audio patterns.
    /// </summary>
    [Test]
    public void Click_TogglesState_BetweenCalls()
    {
        // Act - Generate two clicks and flush them separately
        speaker.Click();
        speaker.Flush();
        var firstClickSamples = testAudioOutput.LastSamples;

        testAudioOutput.Clear();

        speaker.Click();
        speaker.Flush();
        var secondClickSamples = testAudioOutput.LastSamples;

        // Assert - Verify we got samples from both clicks
        Assert.That(firstClickSamples, Is.Not.Null, "First click should generate samples");
        Assert.That(secondClickSamples, Is.Not.Null, "Second click should generate samples");

        // The samples should differ due to state toggle (positive vs negative amplitude)
        if (firstClickSamples != null &&
            secondClickSamples != null &&
            firstClickSamples.Length > 0 &&
            secondClickSamples.Length > 0)
        {
            // Check that at least some samples have different signs (indicating state toggle)
            var firstSign = Math.Sign(firstClickSamples[firstClickSamples.Length / 2]);
            var secondSign = Math.Sign(secondClickSamples[secondClickSamples.Length / 2]);
            Assert.That(
                firstSign,
                Is.Not.EqualTo(secondSign),
                "Speaker state should toggle between clicks");
        }
    }

    /// <summary>
    /// Verifies that the Beep() method generates audio with the correct frequency and duration.
    /// </summary>
    [Test]
    public void Beep_GeneratesCorrectWaveform()
    {
        // Arrange
        const int expectedSampleRate = 44100;
        const double expectedDuration = 0.15; // Based on AppleSpeaker.BeepDuration
        const int expectedFrequency = 800; // Based on AppleSpeaker.BeepFrequency
        int expectedSampleCount = (int)(expectedSampleRate * expectedDuration);

        // Act
        speaker.Beep();

        // Assert
        var beepSamples = testAudioOutput.LastSamples;
        Assert.That(beepSamples, Is.Not.Null, "Beep should generate audio samples");
        Assert.That(
            beepSamples!.Length,
            Is.EqualTo(expectedSampleCount),
            $"Beep should generate approximately {expectedSampleCount} samples for {expectedDuration}s at {expectedSampleRate}Hz");

        // Verify that the waveform contains a square wave pattern
        // Check that there are transitions between positive and negative values
        int transitionCount = 0;
        for (int i = 1; i < beepSamples.Length; i++)
        {
            if (Math.Sign(beepSamples[i]) != Math.Sign(beepSamples[i - 1]) && beepSamples[i] != 0 && beepSamples[i - 1] != 0)
            {
                transitionCount++;
            }
        }

        // For a square wave at 800Hz for 0.15s, we expect roughly 800 * 0.15 * 2 = 240 transitions
        // Allow for a reasonable range
        int expectedTransitions = expectedFrequency * 2 * (int)expectedDuration;
        Assert.That(
            transitionCount,
            Is.GreaterThan(expectedTransitions / 2),
            "Beep should generate a square wave with appropriate frequency transitions");
    }

    /// <summary>
    /// Verifies that the Flush() method clears the audio buffer.
    /// </summary>
    [Test]
    public void Flush_ClearsAudioBuffer()
    {
        // Arrange
        speaker.Click();

        // Act - First flush should send data
        speaker.Flush();
        var firstCallCount = testAudioOutput.PlayCallCount;

        // Act - Second flush should not send data (buffer is empty)
        speaker.Flush();
        var secondCallCount = testAudioOutput.PlayCallCount;

        // Assert - Second flush should not call Play (call count should remain the same)
        Assert.That(
            secondCallCount,
            Is.EqualTo(firstCallCount),
            "Flush should not send empty buffer");
    }

    /// <summary>
    /// Verifies that concurrent Click() calls are thread-safe and don't corrupt the audio buffer.
    /// </summary>
    [Test]
    public void Click_ConcurrentCalls_AreThreadSafe()
    {
        // Arrange
        const int threadCount = 10;
        const int clicksPerThread = 100;
        var threads = new Thread[threadCount];
        var exceptions = new List<Exception>();

        // Act - Create multiple threads that call Click concurrently
        for (int i = 0; i < threadCount; i++)
        {
            threads[i] = new Thread(() =>
            {
                try
                {
                    for (int j = 0; j < clicksPerThread; j++)
                    {
                        speaker.Click();
                    }
                }
                catch (Exception ex)
                {
                    lock (exceptions)
                    {
                        exceptions.Add(ex);
                    }
                }
            });
        }

        // Start all threads
        foreach (var thread in threads)
        {
            thread.Start();
        }

        // Wait for all threads to complete
        foreach (var thread in threads)
        {
            thread.Join();
        }

        // Flush to ensure all clicks are processed
        speaker.Flush();

        // Assert - No exceptions should have occurred
        Assert.That(exceptions, Is.Empty, "Concurrent Click calls should not throw exceptions");

        // Verify that Play was called (at least once)
        Assert.That(
            testAudioOutput.PlayCallCount,
            Is.GreaterThan(0),
            "Concurrent clicks should generate audio samples");
    }

    /// <summary>
    /// Verifies that the Dispose() method properly releases resources.
    /// </summary>
    [Test]
    public void Dispose_ReleasesResources()
    {
        // Act
        speaker.Dispose();

        // Assert - Verify that audio output was disposed
        Assert.That(testAudioOutput.IsDisposed, Is.True, "Dispose should release audio output");

        // Verify that subsequent calls to Click don't throw and don't generate audio
        Assert.DoesNotThrow(() => speaker.Click(), "Click after Dispose should not throw");

        // Reset call count to verify no new audio is generated
        testAudioOutput.Clear();
        speaker.Click();
        speaker.Flush();

        Assert.That(
            testAudioOutput.PlayCallCount,
            Is.EqualTo(0),
            "Click after Dispose should not generate audio");
    }

    /// <summary>
    /// Verifies that Dispose() can be called multiple times safely (idempotent).
    /// </summary>
    [Test]
    public void Dispose_CanBeCalledMultipleTimes()
    {
        // Act & Assert - Multiple Dispose calls should not throw
        Assert.DoesNotThrow(
            () =>
            {
                speaker.Dispose();
                speaker.Dispose();
                speaker.Dispose();
            },
            "Multiple Dispose calls should be safe");
    }

    /// <summary>
    /// Verifies that Beep() generates audio with proper envelope (attack and release).
    /// </summary>
    [Test]
    public void Beep_HasProperEnvelope()
    {
        // Act
        speaker.Beep();

        // Assert
        var beepSamples = testAudioOutput.LastSamples;
        Assert.That(beepSamples, Is.Not.Null, "Beep should generate samples");

        if (beepSamples != null && beepSamples.Length > 100)
        {
            // Check that the beginning has lower amplitude (attack)
            var startAmplitude = Math.Abs(beepSamples[10]);
            var middleAmplitude = Math.Abs(beepSamples[beepSamples.Length / 2]);
            var endAmplitude = Math.Abs(beepSamples[beepSamples.Length - 10]);

            // Middle should have higher amplitude than start and end
            Assert.That(
                middleAmplitude,
                Is.GreaterThan(startAmplitude),
                "Beep should have attack envelope (start quieter than middle)");
            Assert.That(
                middleAmplitude,
                Is.GreaterThan(endAmplitude),
                "Beep should have release envelope (end quieter than middle)");
        }
    }

    /// <summary>
    /// Verifies that Click() after Beep() works correctly.
    /// </summary>
    [Test]
    public void Click_AfterBeep_WorksCorrectly()
    {
        // Arrange
        testAudioOutput.Clear();

        // Act
        speaker.Beep();
        speaker.Click();
        speaker.Flush();

        // Assert - Both Beep and Click should generate audio
        Assert.That(
            testAudioOutput.PlayCallCount,
            Is.GreaterThanOrEqualTo(2),
            "Both Beep and Click should generate audio");
    }

    /// <summary>
    /// Verifies that Flush() does nothing when the buffer is already empty.
    /// </summary>
    [Test]
    public void Flush_WhenBufferEmpty_DoesNothing()
    {
        // Arrange - Start with empty buffer
        testAudioOutput.Clear();

        // Act
        speaker.Flush();

        // Assert
        Assert.That(
            testAudioOutput.PlayCallCount,
            Is.EqualTo(0),
            "Flush with empty buffer should not call Play");
    }

    /// <summary>
    /// Test implementation of IAudioOutput for capturing audio samples.
    /// </summary>
    private class TestAudioOutput : IAudioOutput
    {
        public int PlayCallCount { get; private set; }

        public short[]? LastSamples { get; private set; }

        public bool IsDisposed { get; private set; }

        public void Play(short[] samples)
        {
            PlayCallCount++;
            LastSamples = (short[])samples.Clone();
        }

        public void Dispose()
        {
            IsDisposed = true;
        }

        public void Clear()
        {
            PlayCallCount = 0;
            LastSamples = null;
        }
    }
}

