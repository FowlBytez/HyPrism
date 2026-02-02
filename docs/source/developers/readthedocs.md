# Read the Docs integration

This page explains how HyPrism is configured for Read the Docs and how to update or troubleshoot the docs build.

## What is `readthedocs.yml`?

`readthedocs.yml` (stored at the repository root) tells Read the Docs how to build the documentation for this project. Our file configures:

- The Sphinx configuration file (`docs/conf.py`) to use
- Python dependencies to install from `docs/requirements.txt`
- Build formats and settings

## Local testing

To validate docs locally:

1. Create a virtualenv and install requirements:
```bash
python -m venv .venv
source .venv/bin/activate
pip install -r docs/requirements.txt
```
2. Build the HTML locally:
```bash
cd docs
make html
# open _build/html/index.html
```

If Sphinx or MyST raises errors, fix the Markdown/RST files and re-run the build until it succeeds.

## Updating docs build config

- If you add new Python requirements for the docs (extensions, linters, spellcheckers), add them to `docs/requirements.txt` and update `readthedocs.yml` if you need a different Python runtime or special build options.
- When changing `docs/conf.py` or Sphinx extensions, run a local build to ensure the docs still compile.

## Automation guidance

- When changing docs content or build config, include a docs checklist in your PR and ensure the docs build passes in CI or on Read the Docs.
- See `AGENTS.md` for agent guidelines about mandatory doc updates.
