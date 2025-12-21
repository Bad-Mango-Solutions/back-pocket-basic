# Security Policy

## Supported Versions

We release updates and security patches for the following versions:

| Version | Supported          |
| ------- | ------------------ |
| Latest (main branch) | :white_check_mark: |
| Older commits | :x: |

We recommend always using the latest version from the `main` branch.

## Reporting a Vulnerability

If you discover a security vulnerability in this project, please help us by reporting it responsibly.

### How to Report

**Please do NOT open a public issue for security vulnerabilities.**

Instead, please report security vulnerabilities through one of the following methods:

1. **GitHub Security Advisory** (Preferred)
   - Navigate to the [Security tab](https://github.com/Bad-Mango-Solutions/back-pocket-basic/security/advisories)
   - Click "Report a vulnerability"
   - Provide detailed information about the vulnerability

2. **Email**
   - Contact the repository owner directly through GitHub
   - Use the subject line: "Security Vulnerability in applesoft-basic"

### What to Include

When reporting a security vulnerability, please include:

- **Description**: Clear description of the vulnerability
- **Impact**: What could an attacker do with this vulnerability?
- **Reproduction**: Steps to reproduce the vulnerability
- **Affected Versions**: Which versions are affected?
- **Suggested Fix**: If you have ideas on how to fix it (optional)
- **Proof of Concept**: Code demonstrating the vulnerability (if safe to share)

### What to Expect

- **Acknowledgment**: We will acknowledge receipt within 48 hours
- **Assessment**: We will assess the vulnerability and determine severity
- **Timeline**: We will provide an expected timeline for a fix
- **Updates**: We will keep you informed of progress
- **Credit**: With your permission, we will credit you in the security advisory

### Security Update Process

1. Vulnerability reported and confirmed
2. Fix developed and tested
3. Security advisory published
4. Fix released and merged to main branch
5. Public disclosure (coordinated with reporter)

## Security Best Practices

When using this interpreter:

### Input Validation
- Be cautious when running BASIC programs from untrusted sources
- Review code before execution, especially `POKE` and `CALL` statements
- The interpreter includes bounds checking, but complex programs should be reviewed

### Memory Safety
- The emulated 6502 memory space (64KB) is isolated from system memory
- `PEEK` and `POKE` operate within the emulated memory only
- `CALL` executes emulated 6502 instructions, not native code

### File System Access
- Currently, the interpreter has limited file system access
- Only specified BASIC program files are loaded
- No arbitrary file I/O (future feature may require additional security)

### Dependencies
- Regularly update .NET runtime to the latest secure version
- Monitor Dependabot alerts for dependency vulnerabilities
- Review and merge security updates promptly

## Known Security Considerations

### Emulation Boundaries
- The 6502 CPU emulation is sandboxed
- Memory operations are bounds-checked
- No direct system calls from BASIC code

### Resource Limits
- Consider implementing execution time limits for long-running programs
- Monitor memory usage for programs with large arrays
- Be aware of infinite loops in BASIC programs

## Security Disclosure History

No security vulnerabilities have been reported to date.

## Additional Resources

- [GitHub Security Best Practices](https://docs.github.com/en/code-security)
- [.NET Security Guidelines](https://docs.microsoft.com/en-us/dotnet/standard/security/)
- [OWASP Top Ten](https://owasp.org/www-project-top-ten/)

## Questions?

If you have questions about this security policy, please open an issue or contact the maintainer.

---

**Last Updated**: December 2024
