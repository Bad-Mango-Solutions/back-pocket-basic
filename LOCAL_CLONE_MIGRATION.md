# Local Clone Migration Guide

## Repository Transfer Complete

The repository has been transferred from `jpactor/applesoft-basic` to `Bad-Mango-Solutions/back-pocket-basic`.

## Migrating Your Local Clone

If you have an existing local clone of the repository, follow these steps to update it to point to the new location:

### Option 1: Update Remote URL (Recommended)

This preserves your local branches and commit history:

```bash
# Navigate to your local repository
cd /path/to/your/local/clone

# Update the origin remote URL
git remote set-url origin https://github.com/Bad-Mango-Solutions/back-pocket-basic.git

# Verify the new URL
git remote -v
```

**Expected output:**
```
origin  https://github.com/Bad-Mango-Solutions/back-pocket-basic.git (fetch)
origin  https://github.com/Bad-Mango-Solutions/back-pocket-basic.git (push)
```

```bash
# Fetch the latest changes
git fetch origin

# Update your local main branch
git checkout main
git pull origin main
```

### Option 2: Fresh Clone

If you prefer to start fresh:

```bash
# Clone the repository at the new location
git clone https://github.com/Bad-Mango-Solutions/back-pocket-basic.git
cd back-pocket-basic

# Build and verify
dotnet restore BackPocketBasic.slnx
dotnet build BackPocketBasic.slnx
dotnet test BackPocketBasic.slnx
```

### For Contributors with Forks

If you forked the repository before the transfer:

1. **Update your fork's upstream remote:**
   ```bash
   cd /path/to/your/fork
   git remote set-url upstream https://github.com/Bad-Mango-Solutions/back-pocket-basic.git
   git remote -v
   ```

2. **Sync with upstream:**
   ```bash
   git fetch upstream
   git checkout main
   git merge upstream/main
   git push origin main
   ```

3. **Consider re-forking:**
   - You may want to create a new fork from `Bad-Mango-Solutions/back-pocket-basic`
   - This ensures your fork is properly linked to the new organization

### Verifying the Migration

After updating your remote URL:

1. **Check remote configuration:**
   ```bash
   git remote -v
   ```
   Should show `Bad-Mango-Solutions/back-pocket-basic`

2. **Verify you can fetch:**
   ```bash
   git fetch origin
   ```

3. **Check branch tracking:**
   ```bash
   git branch -vv
   ```
   Should show branches tracking `origin/main`

4. **Test push access** (if you have write permissions):
   ```bash
   git push origin main --dry-run
   ```

### Troubleshooting

#### "Repository not found" error

If you get this error after updating the remote URL:
- Verify you're using the correct URL: `https://github.com/Bad-Mango-Solutions/back-pocket-basic.git`
- Check if you have access to the repository
- Ensure you're authenticated with GitHub

#### GitHub automatically redirects old URLs

GitHub provides automatic redirects for transferred repositories, so:
- `https://github.com/jpactor/applesoft-basic.git` will redirect to `https://github.com/Bad-Mango-Solutions/back-pocket-basic.git`
- However, it's still best practice to update your remote URL explicitly

### What Changed

- **Organization**: `jpactor` → `Bad-Mango-Solutions`
- **Repository name**: `applesoft-basic` → `back-pocket-basic`
- **Full URL**: `https://github.com/jpactor/applesoft-basic` → `https://github.com/Bad-Mango-Solutions/back-pocket-basic`

### What Stayed the Same

- All commit history is preserved
- All branches remain intact
- All issues, PRs, and discussions are preserved
- All releases and tags are preserved
- The default branch is still `main`

## Questions?

If you encounter any issues migrating your local clone, please open an issue at:
https://github.com/Bad-Mango-Solutions/back-pocket-basic/issues

---

**Last Updated**: December 21, 2024
