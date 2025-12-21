# Repository Setup Guide

This guide provides complete instructions for setting up branch protections, configuring permissions, and making this repository public.

## Overview

This guide addresses the requirements to:
1. ✅ Rename the master branch to 'main'
2. ✅ Apply branch protections to 'main'
3. ✅ Provide synchronization instructions for existing clones
4. ✅ Ensure proper repository permissions
5. ✅ Make the repository public

## Quick Start for Repository Owner

Follow these steps in order:

### Phase 1: Branch Migration (Required First)
📖 See [BRANCH_MIGRATION.md](BRANCH_MIGRATION.md) for detailed instructions

1. Rename `master` to `main` on GitHub (Settings → Branches)
2. Update your local repository
3. Post synchronization instructions to any open issues

### Phase 2: Branch Protection (After Migration)
📖 See [BRANCH_PROTECTION.md](BRANCH_PROTECTION.md) for detailed instructions

1. Configure branch protection rules for `main`
2. Enable required status checks
3. Require pull request reviews
4. Test the protections

### Phase 3: Repository Configuration
📖 See [REPOSITORY_SETTINGS.md](REPOSITORY_SETTINGS.md) for detailed instructions

1. Configure access control and permissions
2. Prepare repository for public visibility
3. Make repository public
4. Enable security features

## Detailed Instructions

### Step 1: Rename Master Branch to Main

**Time Required:** 5-10 minutes

1. **On GitHub:**
   - Go to https://github.com/Bad-Mango-Solutions/back-pocket-basic/settings/branches
   - Click the pencil icon next to the default branch
   - Rename to `main`
   - Confirm the action

2. **Update Your Local Repository:**
   ```bash
   cd applesoft-basic
   git fetch origin
   git branch -m master main
   git branch -u origin/main main
   git symbolic-ref refs/remotes/origin/HEAD refs/remotes/origin/main
   ```

3. **Verify:**
   ```bash
   git branch --show-current  # Should show: main
   git pull                    # Should work without errors
   ```

### Step 2: Apply Branch Protections

**Time Required:** 5-10 minutes

1. **Navigate to Branch Protection Settings:**
   - Go to https://github.com/Bad-Mango-Solutions/back-pocket-basic/settings/branch_protection_rules/new

2. **Configure the Protection Rule:**
   - **Branch name pattern:** `main`
   - ✅ Require a pull request before merging
     - Require approvals: 1
     - Dismiss stale pull request approvals when new commits are pushed
   - ✅ Require status checks to pass before merging
     - Require branches to be up to date before merging
     - Add status checks: `build`, `test` (after CI workflow runs once)
   - ✅ Require conversation resolution before merging
   - ❌ Allow force pushes (disabled)
   - ❌ Allow deletions (disabled)
   - ✅ Restrict who can push to matching branches
     - Add: @jpactor
   - ✅ Include administrators (recommended)

3. **Save the Rule:**
   - Click "Create" or "Save changes"

4. **Verify:**
   ```bash
   # Try to push directly to main (should fail)
   git checkout main
   echo "test" >> test.txt
   git add test.txt
   git commit -m "Test protection"
   git push origin main
   # Expected: Error "protected branch hook declined"
   
   # Clean up
   git reset --hard HEAD~1
   ```

### Step 3: Post Issue Comment with Synchronization Instructions

**Copy and paste this message to the issue:**

---

## 📢 Branch Renamed: master → main

The default branch has been renamed from `master` to `main`.

### For Contributors with Existing Clones

If you have an existing clone of this repository, please run these commands:

```bash
# Navigate to your repository
cd applesoft-basic

# Fetch updates from remote
git fetch origin

# Rename your local branch
git branch -m master main

# Set the new upstream
git branch -u origin/main main

# Update the default remote branch pointer
git symbolic-ref refs/remotes/origin/HEAD refs/remotes/origin/main

# Verify the changes
git branch --show-current  # Should output: main
git status                 # Should show clean working tree
```

### For New Clones

New clones will automatically use the `main` branch:

```bash
git clone https://github.com/Bad-Mango-Solutions/back-pocket-basic.git
cd back-pocket-basic
```

### Need Help?

See [BRANCH_MIGRATION.md](BRANCH_MIGRATION.md) for detailed instructions and troubleshooting.

---

### Step 4: Configure Repository Permissions

**Time Required:** 5 minutes

1. **Review Current Access:**
   - Go to https://github.com/Bad-Mango-Solutions/back-pocket-basic/settings/access

2. **Configure Access Levels:**
   - **Owner (@jpactor):** Admin access (already set)
   - **Contributors:** Use fork workflow (no direct access needed)
   
3. **Branch Protection Enforcement:**
   - With branch protections enabled, even users with write access must use PRs
   - Only @jpactor can push to protected `main` branch

### Step 5: Make Repository Public

**Time Required:** 10-15 minutes (including pre-checks)

**⚠️ Important: Complete Pre-Public Checklist First**

1. **Pre-Public Security Scan:**
   ```bash
   # Check for exposed secrets
   cd applesoft-basic
   git log -p | grep -iE "(password|secret|api_key|token|credential)" || echo "No obvious secrets found"
   
   # Review .gitignore
   cat .gitignore
   ```

2. **Pre-Public Checklist:**
   - ✅ No secrets in commit history
   - ✅ LICENSE file present (MIT)
   - ✅ README.md is comprehensive
   - ✅ .gitignore configured properly
   - ✅ Branch protections configured
   - ✅ CI/CD workflow added

3. **Make Public:**
   - Go to https://github.com/Bad-Mango-Solutions/back-pocket-basic/settings
   - Scroll to "Danger Zone"
   - Click "Change visibility"
   - Select "Make public"
   - Type repository name to confirm
   - Click "I understand, make this repository public"

4. **Post-Public Configuration:**
   
   **Enable Security Features:**
   - Go to https://github.com/Bad-Mango-Solutions/back-pocket-basic/settings/security_analysis
   - ✅ Enable "Dependabot alerts"
   - ✅ Enable "Dependabot security updates"
   - ✅ Enable "Code scanning" (CodeQL)
   - ✅ Enable "Secret scanning" (automatic for public repos)

   **Update Repository Details:**
   - Go to repository main page
   - Click gear icon next to "About"
   - Add description: "BackPocketBASIC - Applesoft BASIC interpreter in .NET with 6502 CPU emulation"
   - Add topics: `applesoft-basic`, `apple-ii`, `6502`, `emulator`, `interpreter`, `dotnet`, `csharp`, `retro-computing`
   - Save changes

## CI/CD Integration

This repository includes a GitHub Actions workflow (`.github/workflows/ci.yml`) that runs:

- **Build**: Compiles the .NET solution
- **Test**: Runs unit tests
- **Code Quality**: Performs static analysis

### First Workflow Run

After pushing this PR:
1. The workflow will run automatically
2. Check the "Actions" tab on GitHub
3. Once successful, the status checks will appear in branch protection settings

### Adding Status Checks to Branch Protection

After the workflow runs successfully once:
1. Go to branch protection settings
2. Under "Require status checks to pass before merging"
3. Search for and add:
   - `build`
   - `test`
   - `code-quality`
4. Save the rule

## Verification Checklist

After completing all steps, verify:

### Branch Migration
- [ ] Default branch is `main` on GitHub
- [ ] Local repository updated to `main`
- [ ] Can pull/push to `main` successfully
- [ ] Remote tracking configured correctly

### Branch Protection
- [ ] Protection rule exists for `main`
- [ ] Cannot push directly to `main`
- [ ] Pull requests required
- [ ] Status checks required (after CI runs)
- [ ] Force push disabled
- [ ] Branch deletion disabled

### Repository Access
- [ ] Owner has admin access
- [ ] Branch protections apply to admins
- [ ] No unnecessary write access granted

### Public Repository
- [ ] Repository is public
- [ ] No secrets exposed
- [ ] Security features enabled
- [ ] Repository description and topics set
- [ ] README displays properly

### CI/CD
- [ ] GitHub Actions workflow present
- [ ] Workflow runs successfully
- [ ] Status checks added to branch protection

## Troubleshooting

### "Cannot rename branch" Error
- Check if there are open PRs targeting master
- Close or retarget PRs first
- Try again

### "Branch protection rule not working"
- Verify you're testing on the correct branch
- Check if "Include administrators" is enabled
- Review rule configuration

### "CI workflow not running"
- Check workflow file syntax
- Verify branch name in workflow triggers
- Check Actions tab for error messages

### "Status checks not appearing"
- Workflow must run successfully at least once
- Check exact job names in workflow file
- Refresh branch protection settings page

## Additional Documentation

Detailed guides are available for each topic:

- **[BRANCH_MIGRATION.md](BRANCH_MIGRATION.md)** - Complete branch migration instructions
- **[BRANCH_PROTECTION.md](BRANCH_PROTECTION.md)** - Comprehensive branch protection setup
- **[REPOSITORY_SETTINGS.md](REPOSITORY_SETTINGS.md)** - Repository configuration and security

## Support

If you encounter issues:
1. Review the troubleshooting sections
2. Check GitHub documentation links in each guide
3. Search GitHub community forums
4. Open an issue if problems persist

## Summary

After completing this guide:
- ✅ Your default branch will be `main`
- ✅ Branch protections will enforce code quality standards
- ✅ Contributors will know how to synchronize their clones
- ✅ Only authorized users can modify the repository
- ✅ The repository will be public and secure

## Timeline

Expected time to complete all steps: **30-45 minutes**

- Phase 1 (Branch Migration): 5-10 minutes
- Phase 2 (Branch Protection): 5-10 minutes
- Phase 3 (Repository Setup): 10-15 minutes
- Phase 4 (Make Public): 10-15 minutes

## Next Steps

1. Start with [BRANCH_MIGRATION.md](BRANCH_MIGRATION.md)
2. Then proceed to [BRANCH_PROTECTION.md](BRANCH_PROTECTION.md)
3. Finally, complete [REPOSITORY_SETTINGS.md](REPOSITORY_SETTINGS.md)
4. Mark this issue as complete!
