# Branch Migration Guide: master → main

This guide provides step-by-step instructions for renaming the default branch from `master` to `main` and updating existing clones.

## Repository Owner Instructions

### Step 1: Rename the Branch on GitHub

1. Navigate to your repository on GitHub: https://github.com/jpactor/applesoft-basic
2. Click on **Settings** tab
3. In the left sidebar, click **Branches**
4. Under "Default branch", click the pencil/edit icon next to the current default branch
5. In the dropdown, select or type `main` (you may need to create it first)
6. Click **Update** or **Rename branch**
7. GitHub will show a warning about the impacts. Review and confirm.

Alternatively, you can rename directly:
1. Go to the main repository page
2. Click the branch dropdown (showing current branch name)
3. Click **View all branches**
4. Find the `master` branch
5. Click the pencil icon next to it
6. Enter `main` as the new name
7. Click **Rename branch**

### Step 2: Update Local Repository (Repository Owner)

After renaming on GitHub, update your local repository:

```bash
# Fetch the latest changes from remote
git fetch origin

# Update the local master branch to point to main
git branch -m master main

# Set the upstream to the new main branch
git branch -u origin/main main

# Update the default branch reference
git symbolic-ref refs/remotes/origin/HEAD refs/remotes/origin/main

# Verify the change
git branch -a
```

### Step 3: Communicate the Change

Post the following message to any open issues or as a repository announcement:

---

## 📢 Important: Default Branch Renamed to `main`

The default branch for this repository has been renamed from `master` to `main`.

### For Contributors with Existing Clones

If you have an existing clone of this repository, please run the following commands to update your local repository:

```bash
# Navigate to your local repository
cd applesoft-basic

# Fetch the latest changes
git fetch origin

# Rename your local master branch to main
git branch -m master main

# Update the upstream tracking
git branch -u origin/main main

# Update the symbolic reference
git symbolic-ref refs/remotes/origin/HEAD refs/remotes/origin/main

# Verify the changes
git branch -a
git status
```

### For New Clones

New clones will automatically use the `main` branch:

```bash
git clone https://github.com/jpactor/applesoft-basic.git
cd applesoft-basic
```

### Updating References in Documentation

The README.md has been updated to reflect the new branch name. If you have any bookmarks or CI/CD configurations pointing to `master`, please update them to use `main`.

---

## Instructions for Existing Contributors

If you have an existing clone of this repository, follow these steps to synchronize with the renamed branch:

### Quick Update (Recommended)

```bash
# Navigate to your repository
cd /path/to/applesoft-basic

# Ensure you're on the master branch and have no uncommitted changes
git checkout master
git status

# Fetch updates from remote
git fetch origin

# Rename your local branch
git branch -m master main

# Set the new upstream
git branch -u origin/main main

# Update the default remote branch pointer
git symbolic-ref refs/remotes/origin/HEAD refs/remotes/origin/main

# Verify everything is correct
git status
git branch -vv
```

### Verification Steps

After updating, verify that everything is working correctly:

```bash
# Check current branch
git branch --show-current
# Should output: main

# Check remote tracking
git branch -vv
# Should show: * main [origin/main] ...

# Check remote branches
git branch -r
# Should show origin/main, not origin/master

# Pull latest changes
git pull
# Should work without errors
```

### Cleaning Up Old References

Once you've confirmed everything works, you can remove stale references:

```bash
# Remove the remote tracking branch for master if it still exists
git remote prune origin

# Verify cleanup
git branch -r
```

### Troubleshooting

#### "fatal: A branch named 'main' already exists"

If you already have a local `main` branch:

```bash
# Check if your current main is the same as master
git diff master main

# If they're the same, delete main and retry
git branch -D main
git branch -m master main
git branch -u origin/main main
```

#### "Branch 'master' has no upstream"

If your master branch wasn't tracking a remote:

```bash
git branch -m master main
git branch -u origin/main main
```

#### Pull Requests and Branches

If you have open pull requests based on `master`:
1. Your PR will automatically be updated to target `main`
2. Your feature branch doesn't need changes
3. The base branch reference will be updated by GitHub

### Alternative: Fresh Clone

If you encounter issues, the simplest solution is to clone fresh:

```bash
# Backup any local changes
cd /path/to/applesoft-basic
git stash

# Note your current branch if it's not master
git branch --show-current

# Clone fresh
cd ..
mv applesoft-basic applesoft-basic-old
git clone https://github.com/jpactor/applesoft-basic.git
cd applesoft-basic

# Apply stashed changes if needed
# (from the old directory)
```

## Additional Resources

- [GitHub: Renaming a branch](https://docs.github.com/en/repositories/configuring-branches-and-merges-in-your-repository/managing-branches-in-your-repository/renaming-a-branch)
- [Git: Branch Management](https://git-scm.com/book/en/v2/Git-Branching-Branch-Management)
