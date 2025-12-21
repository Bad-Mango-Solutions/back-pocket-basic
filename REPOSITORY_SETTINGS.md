# Repository Settings and Permissions Guide

This guide provides instructions for configuring repository permissions and making the repository public.

## Table of Contents

1. [Repository Access Control](#repository-access-control)
2. [Making the Repository Public](#making-the-repository-public)
3. [Security Best Practices](#security-best-practices)
4. [Visibility Transition Checklist](#visibility-transition-checklist)

## Repository Access Control

### Overview

Proper access control ensures that only authorized users can modify code while maintaining transparency for contributors.

### Step 1: Review Current Permissions

1. Navigate to your repository: https://github.com/jpactor/applesoft-basic
2. Click on **Settings** tab
3. In the left sidebar, click **Collaborators and teams**
4. Review existing access levels

### Step 2: Configure Access Levels

GitHub provides several permission levels:

| Permission Level | Capabilities |
|-----------------|--------------|
| **Read** | View and clone repository, open issues, comment |
| **Triage** | Read + manage issues and pull requests without write access |
| **Write** | Read + push to repository, merge pull requests |
| **Maintain** | Write + manage repository settings (without sensitive access) |
| **Admin** | Full access including repository settings and deletion |

### Recommended Configuration

#### For Repository Owner
- **@jpactor**: **Admin** access
- Maintains full control over repository settings, permissions, and content

#### For Core Contributors (if any)
- **Maintain** or **Write** access
- Can merge PRs and manage issues
- Cannot modify critical settings or delete repository

#### For Regular Contributors
- No direct access needed (use fork workflow)
- Can submit pull requests from forks
- Pull requests reviewed before merging

### Step 3: Restrict Direct Push Access

To ensure all changes go through pull requests:

1. Navigate to **Settings** → **Branches**
2. Configure branch protection rules (see BRANCH_PROTECTION.md)
3. Enable "Restrict who can push to matching branches"
4. Add only trusted users:
   - Repository owner (@jpactor)
   - Core maintainers (if any)

Even with write access, users must follow PR workflow when branch protection is enabled.

### Step 4: Teams Configuration (for Organizations)

If this repository is under an organization:

1. Navigate to **Settings** → **Collaborators and teams**
2. Click **Add teams**
3. Create teams with appropriate access:
   - **Maintainers**: Write or Maintain access
   - **Contributors**: Read access
   - **Bots**: Limited access for CI/CD

## Making the Repository Public

### Pre-Public Checklist

Before making the repository public, ensure:

- [ ] No secrets or credentials in commit history
- [ ] LICENSE file is present and appropriate (✓ MIT License exists)
- [ ] README.md is comprehensive and welcoming (✓ Exists)
- [ ] Code is in a presentable state
- [ ] .gitignore excludes sensitive files
- [ ] Branch protections are configured
- [ ] All issues contain appropriate information
- [ ] Any sensitive discussions are resolved

### Step 1: Scan for Secrets

Run a security scan to detect any exposed secrets:

```bash
# Check for common patterns of secrets
git log -p | grep -iE "(password|secret|api_key|token|credential)" || echo "No obvious secrets found"

# Review .gitignore
cat .gitignore
```

If secrets are found:
1. Rotate/invalidate the exposed secrets immediately
2. Consider using [BFG Repo-Cleaner](https://rtyley.github.io/bfg-repo-cleaner/) to remove from history
3. Force push the cleaned history (before making public)

### Step 2: Make Repository Public

1. Navigate to your repository: https://github.com/jpactor/applesoft-basic
2. Click on **Settings** tab
3. Scroll down to the **Danger Zone** section
4. Click **Change visibility**
5. Select **Make public**
6. Read the warnings carefully
7. Type the repository name to confirm
8. Click **I understand, make this repository public**

### Step 3: Post-Public Configuration

After making the repository public:

#### Enable Useful Features

1. **Discussions** (optional)
   - Settings → General → Features → Enable Discussions
   - Provides a community forum for questions

2. **Wikis** (optional)
   - Settings → General → Features → Enable Wikis
   - For extended documentation

3. **Projects** (optional)
   - Settings → General → Features → Enable Projects
   - For project management

4. **Sponsorships** (optional)
   - Settings → General → Sponsorships
   - Allow users to sponsor the project

#### Configure Issue Templates

Create `.github/ISSUE_TEMPLATE/` directory with templates for:
- Bug reports
- Feature requests
- Questions

#### Configure Pull Request Template

Create `.github/PULL_REQUEST_TEMPLATE.md` to guide contributors

### Step 4: Update Repository Description

1. Go to the repository main page
2. Click the gear icon next to "About"
3. Add a short description: "BackPocketBASIC - Applesoft BASIC interpreter in .NET with 6502 CPU emulation"
4. Add topics/tags:
   - `applesoft-basic`
   - `apple-ii`
   - `6502`
   - `emulator`
   - `interpreter`
   - `dotnet`
   - `csharp`
   - `retro-computing`
5. Add website (if applicable)
6. Save changes

### Step 5: Announce the Public Release

Consider announcing on:
- Repository README.md (add a badge)
- Social media
- Relevant forums (r/apple2, r/vintageapple, r/retrobattlestations)
- Dev.to or similar platforms

## Security Best Practices

### Enable Security Features

1. **Dependabot Alerts**
   - Settings → Code security and analysis
   - Enable "Dependabot alerts"
   - Enable "Dependabot security updates"

2. **Code Scanning**
   - Settings → Code security and analysis
   - Enable "Code scanning"
   - Setup GitHub Advanced Security (CodeQL)

3. **Secret Scanning**
   - Settings → Code security and analysis
   - Enable "Secret scanning"
   - Automatically enabled for public repositories

### Security Policy

Create `.github/SECURITY.md` to document:
- Supported versions
- How to report vulnerabilities
- Security update process

Example:
```markdown
# Security Policy

## Reporting a Vulnerability

If you discover a security vulnerability, please email:
security@example.com

Please do not open public issues for security vulnerabilities.

## Supported Versions

| Version | Supported          |
| ------- | ------------------ |
| Latest  | :white_check_mark: |
| Older   | :x:                |
```

### Regular Maintenance

- Monitor dependency alerts
- Review and accept Dependabot PRs promptly
- Keep dependencies updated
- Review code scanning alerts

## Visibility Transition Checklist

Use this checklist when transitioning from private to public:

### Pre-Transition
- [ ] Audit commit history for secrets
- [ ] Verify LICENSE file exists and is appropriate
- [ ] Ensure README.md is complete and accurate
- [ ] Configure .gitignore properly
- [ ] Remove any sensitive information from issues/PRs
- [ ] Configure branch protections
- [ ] Set up CI/CD workflows
- [ ] Review all open issues for sensitive content

### During Transition
- [ ] Scan for secrets one final time
- [ ] Make repository public through Settings
- [ ] Update repository description and topics
- [ ] Enable Dependabot and security scanning
- [ ] Verify branch protections are active

### Post-Transition
- [ ] Monitor for new issues and PRs
- [ ] Respond to community feedback
- [ ] Set up issue/PR templates
- [ ] Create CONTRIBUTING.md guidelines
- [ ] Create CODE_OF_CONDUCT.md
- [ ] Consider adding SECURITY.md
- [ ] Announce the public release
- [ ] Star your own repository (makes it appear more legitimate)

## Access Control Summary

### Current Configuration Goal

| User/Team | Access Level | Rationale |
|-----------|-------------|-----------|
| @jpactor (owner) | Admin | Full control over repository |
| Core maintainers | Write/Maintain | Can merge PRs, manage issues |
| Contributors | None (fork workflow) | Submit PRs from personal forks |
| Public | Read | View code, open issues, submit PRs |

### Enforcement Mechanisms

1. **Branch Protection Rules**: Require PR workflow even for those with write access
2. **Required Reviews**: Minimum 1 approval before merge
3. **Status Checks**: CI/CD must pass before merge
4. **Restricted Push**: Only owner/maintainers can push to protected branches

## Additional Resources

- [GitHub: Setting repository visibility](https://docs.github.com/en/repositories/managing-your-repositorys-settings-and-features/managing-repository-settings/setting-repository-visibility)
- [GitHub: Permission levels for a user account repository](https://docs.github.com/en/account-and-profile/setting-up-and-managing-your-personal-account-on-github/managing-personal-account-settings/permission-levels-for-a-personal-account-repository)
- [GitHub: Securing your repository](https://docs.github.com/en/code-security/getting-started/securing-your-repository)
- [GitHub: Best practices for securing your account and organization](https://docs.github.com/en/code-security/supply-chain-security/end-to-end-supply-chain/securing-accounts)

## Rollback Procedures

If you need to make the repository private again:

1. Navigate to Settings
2. Scroll to Danger Zone
3. Click "Change visibility"
4. Select "Make private"
5. Confirm the action

Note: Some features (like GitHub Actions minutes) may be affected by visibility changes.
