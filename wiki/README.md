# Wiki Documentation

This directory contains the wiki documentation for the BackPocketBASIC project.

## About This Wiki

GitHub wikis are typically stored in a separate git repository (`.wiki.git`). Since we cannot directly push to GitHub's wiki repository from this environment, this `wiki/` directory contains all the wiki content as markdown files that can be:

1. **Copied to the GitHub Wiki**: Upload these files to the actual GitHub wiki
2. **Used as Documentation**: Serve as in-repository documentation
3. **Version Controlled**: Track changes alongside code

## Wiki Structure

### Getting Started
- **Home.md** - Wiki home page and introduction
- **Installation.md** - Prerequisites and installation steps
- **Quick-Start.md** - Running your first BASIC program

### User Guide
- **Language-Reference.md** - Complete command reference
- **Built-in-Functions.md** - Math, string, and utility functions
- **Custom-Extensions.md** - SLEEP command and extensions
- **Sample-Programs.md** - Guided walkthroughs of examples

### Technical Documentation
- **Architecture-Overview.md** - Project structure and components
- **6502-Emulation.md** - CPU emulation details
- **Memory-Map.md** - Detailed memory layout
- **API-Reference.md** - Library integration guide

### Contributing
- **Development-Setup.md** - Development environment setup
- **Testing-Guide.md** - Running and writing tests
- **Code-Style.md** - Coding standards

### Navigation
- **_Sidebar.md** - Wiki sidebar navigation
- **_Footer.md** - Wiki footer

## Copying to GitHub Wiki

To upload these files to the actual GitHub wiki:

### Method 1: Clone and Copy (Recommended)

```bash
# Clone the wiki repository
git clone https://github.com/Bad-Mango-Solutions/back-pocket-basic.wiki.git

# Copy files from this directory
cp wiki/*.md back-pocket-basic.wiki/

# Commit and push
cd back-pocket-basic.wiki
git add .
git commit -m "Initialize wiki documentation"
git push origin master
```

### Method 2: Manual Upload

1. Go to the repository wiki: https://github.com/Bad-Mango-Solutions/back-pocket-basic/wiki
2. Create each page using the wiki editor
3. Copy content from corresponding `.md` file
4. Save each page

### Method 3: Using GitHub Web Interface

1. Navigate to repository Settings → Features
2. Enable Wiki if not already enabled
3. Clone wiki repository
4. Copy files as in Method 1

## File Naming Convention

GitHub wiki pages use specific naming:
- Spaces become dashes: `Quick Start` → `Quick-Start.md`
- Special pages: `_Sidebar.md`, `_Footer.md`
- Home page: `Home.md`

These files already follow the correct naming convention.

## Updating the Wiki

When updating wiki content:

1. **Edit files in this directory** - Keep wiki under version control
2. **Test locally** - Preview markdown to ensure formatting
3. **Commit changes** - Track changes in git
4. **Sync to GitHub wiki** - Upload updated files to wiki repo

## Links in Wiki

Internal wiki links use this format:
```markdown
[Link Text](Page-Name)
```

For example:
- `[Installation](Installation)` → Links to Installation.md
- `[Quick Start](Quick-Start)` → Links to Quick-Start.md

External links use full URLs:
```markdown
[GitHub Repo](https://github.com/Bad-Mango-Solutions/back-pocket-basic)
```

## Maintaining the Wiki

### Adding New Pages

1. Create new `.md` file in this directory
2. Follow existing naming convention
3. Add link to `_Sidebar.md`
4. Add references from related pages
5. Sync to GitHub wiki

### Updating Existing Pages

1. Edit the `.md` file
2. Test markdown rendering
3. Commit changes
4. Sync to GitHub wiki

### Checking Links

Before syncing to wiki, verify:
- Internal links use correct page names
- No broken links
- External links are valid
- Code examples are accurate

## Preview Locally

To preview markdown files:

**VS Code:**
- Install "Markdown All in One" extension
- Press Ctrl+Shift+V to preview

**Command Line:**
```bash
# Install grip (GitHub README instant preview)
pip install grip

# Preview a file
grip wiki/Home.md
```

**Online:**
- Use [StackEdit](https://stackedit.io/)
- Use [Dillinger](https://dillinger.io/)

## Wiki Best Practices

1. **Keep it Current** - Update wiki with code changes
2. **Cross-Reference** - Link related pages
3. **Use Examples** - Include code samples
4. **Be Concise** - Clear, focused content
5. **Check Grammar** - Proofread before publishing

## Contact

Questions about the wiki? Open an issue on GitHub:
https://github.com/Bad-Mango-Solutions/back-pocket-basic/issues
