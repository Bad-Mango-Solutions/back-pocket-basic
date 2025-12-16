# Branch Protection Configuration Guide

This guide provides instructions for configuring branch protections on the `main` branch to ensure code quality and security.

## Overview

Branch protection rules help maintain code quality by requiring certain conditions to be met before changes can be merged into protected branches.

## Repository Owner Instructions

### Step 1: Access Branch Protection Settings

1. Navigate to your repository on GitHub: https://github.com/jpactor/applesoft-basic
2. Click on **Settings** tab
3. In the left sidebar, click **Branches**
4. Under "Branch protection rules", click **Add rule** or **Add branch protection rule**

### Step 2: Configure Protection Rule for `main`

In the "Branch name pattern" field, enter: `main`

### Step 3: Recommended Protection Settings

Enable the following settings for robust protection:

#### ✅ Require a pull request before merging
- **Purpose**: Prevents direct pushes to main, all changes must go through PR review
- **Sub-options**:
  - ☑️ **Require approvals**: Set to at least **1** approval
  - ☑️ **Dismiss stale pull request approvals when new commits are pushed**
  - ☑️ **Require review from Code Owners** (if you have a CODEOWNERS file)

#### ✅ Require status checks to pass before merging
- **Purpose**: Ensures all CI/CD checks pass before merging
- **Sub-options**:
  - ☑️ **Require branches to be up to date before merging**
  - Add status checks once you have CI/CD workflows:
    - `build`
    - `test`
    - `codeql` (if using CodeQL scanning)

#### ✅ Require conversation resolution before merging
- **Purpose**: All PR comments must be resolved before merging

#### ✅ Require signed commits (Optional but recommended)
- **Purpose**: Ensures all commits are signed with GPG keys for authenticity

#### ✅ Require linear history (Optional)
- **Purpose**: Prevents merge commits, requires rebase or squash merging
- **Note**: This enforces a clean, linear commit history

#### ✅ Include administrators
- **Purpose**: Applies these rules to repository administrators as well
- **Recommendation**: Enable this to ensure consistency

#### ⚠️ Do not allow bypassing the above settings
- **Purpose**: Ensures rules cannot be bypassed
- **Note**: Consider carefully as this may limit emergency fixes

#### ✅ Restrict who can push to matching branches
- **Purpose**: Only specified users or teams can push directly
- **Configuration**:
  - Add trusted users or teams
  - Repository owner should be included
  - Consider creating a "maintainers" team

#### ✅ Allow force pushes
- **Recommendation**: **Disable** (leave unchecked)
- **Purpose**: Prevents history rewriting on protected branch

#### ✅ Allow deletions
- **Recommendation**: **Disable** (leave unchecked)
- **Purpose**: Prevents accidental deletion of the main branch

### Step 4: Save Protection Rule

1. Review all settings
2. Click **Create** or **Save changes**
3. Verify the rule appears in the "Branch protection rules" list

## Minimal Protection Configuration

If you want to start with basic protections and enhance later:

### Essential Settings:
- ✅ Require a pull request before merging (at least 1 approval)
- ✅ Require status checks to pass before merging
- ✅ Require conversation resolution before merging
- ❌ Allow force pushes (disabled)
- ❌ Allow deletions (disabled)

## Testing Branch Protections

After configuration, verify protections are working:

```bash
# Try to push directly to main (should fail)
git checkout main
echo "test" >> test.txt
git add test.txt
git commit -m "Test direct push"
git push origin main
# Expected: Error indicating branch is protected
```

## CI/CD Integration

This repository includes a GitHub Actions workflow that enforces quality checks. Once configured, the following status checks will run:

- **Build**: Compiles the solution using `dotnet build`
- **Test**: Runs unit tests using `dotnet test`
- **Code Quality**: Performs static analysis

To require these checks in branch protection:
1. Let the workflow run at least once
2. Return to branch protection settings
3. Under "Require status checks to pass before merging"
4. Search for and add the check names (e.g., `build`, `test`)

## CODEOWNERS File (Optional)

Create a `.github/CODEOWNERS` file to automatically request reviews from specific users or teams:

```
# Default owner for everything in the repo
*       @jpactor

# Specific paths
/src/   @jpactor
/tests/ @jpactor
*.md    @jpactor
```

## Workflow for Contributors

With branch protections enabled, the contribution workflow becomes:

1. **Fork the repository** (or create a feature branch)
2. **Make changes** in a feature branch
3. **Push changes** to the feature branch
4. **Open a Pull Request** targeting `main`
5. **Wait for CI checks** to pass
6. **Request review** from maintainers
7. **Address feedback** if any
8. **Merge** once approved and all checks pass

## Monitoring and Maintenance

### Review Protection Effectiveness
- Regularly review the "Insights" → "Security" tab
- Check "Pull requests" for merge patterns
- Ensure rules are not being bypassed

### Update Rules as Needed
- Add new status checks as CI/CD evolves
- Adjust approval requirements based on team size
- Enable additional protections as the project matures

### Emergency Procedures
If you need to make an emergency fix:
1. Temporarily disable specific rules (requires admin)
2. Make the fix
3. Re-enable protections immediately
4. Document the exception and reason

## Additional Resources

- [GitHub: About protected branches](https://docs.github.com/en/repositories/configuring-branches-and-merges-in-your-repository/managing-protected-branches/about-protected-branches)
- [GitHub: Managing a branch protection rule](https://docs.github.com/en/repositories/configuring-branches-and-merges-in-your-repository/managing-protected-branches/managing-a-branch-protection-rule)
- [Best Practices for Branch Protection](https://docs.github.com/en/repositories/configuring-branches-and-merges-in-your-repository/managing-protected-branches/about-protected-branches#best-practices)

## Summary Checklist

Use this checklist when configuring branch protections:

- [ ] Navigate to Settings → Branches
- [ ] Add branch protection rule for `main`
- [ ] Enable "Require a pull request before merging" (1+ approvals)
- [ ] Enable "Require status checks to pass before merging"
- [ ] Enable "Require conversation resolution before merging"
- [ ] Disable "Allow force pushes"
- [ ] Disable "Allow deletions"
- [ ] Configure "Restrict who can push to matching branches"
- [ ] Save the rule
- [ ] Test by attempting direct push to main
- [ ] Document the configuration for team members
