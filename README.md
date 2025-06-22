# hum

![hummingbird](Assets/hum.png)

A hummingbird-fast CLI to bootstrap new apps from zero to production.

## Table of Contents

- [What is hum?](#what-is-hum)
- [Key Features](#key-features)
- [Architecture Overview](#architecture-overview)
- [Prerequisites](#prerequisites)
- [Installation](#installation)
- [Quick Start](#quick-start)
- [Commands](#commands)
- [Configuration](#configuration)
- [How It Works](#how-it-works)
- [Roadmap](#roadmap)
- [Contributing](#contributing)
- [License](#license)

What is hum?

hum is a global .NET tool that automates the boilerplate required to ship a new service in an opinionated GitHub → GitHub Actions → Ansible deployment pipeline.

With one command you get:

A fresh GitHub repository generated from a template you choose.

A ready‑to‑run GitHub Actions workflow with build, test, and deploy stages wired‑up.

Inventory scaffolding in your infrastructure-as‑code repo so Ansible knows where to deploy.

Optional AWX/Tower job template for click‑to‑deploy convenience.

First‑class secrets management hooks (GitHub Secrets for CI, Ansible Vault or external back‑end for runtime).

Stop copying YAML around—hum turns “new idea” into “running in prod” in < 1 minute.

Key Features

Category

Details

Repo bootstrap

gh API integration, branch‑protection rules, default labels, CODEOWNERS

CI templates

Opinionated workflows for .NET 8+, container & non‑container builds, SCA/SAST slots

Inventory sync

Updates group/host vars and encrypts sensitive values via Ansible Vault

Server binding

Auto‑detect free app servers (or pick one), tags them with the new app

Pluggable

Hooks for custom code generators, alternate IaC back‑ends, Helm, Argo CD

Written in C#

Distributed as a global dotnet tool: dotnet tool install -g hum

Architecture Overview

flowchart LR
  dev["Developer CLI<br/>hum"] --&gt; gh["GitHub<br/>New Repo"]
  gh --&gt; gha["GitHub Actions<br/>(template workflow)"]
  hum --&gt; inv["Ansible Inventory<br/>Pull Request"]
  gha -- Trigger --> awx["Ansible AWX / CLI"]
  awx -- Deploy --> srv["App Server<br/>NGINX · systemd · SQLite"]
  srv -- Backups --> cpsvc["cp Backup Service"]

Note – The diagram is generated, commit it to /docs/architecture.md for richer docs.

Prerequisites

Tool

Min Version

Notes

.NET SDK

8.0

To run the CLI itself

GitHub account

n/a

Personal Access Token with repo, workflow, actions:read

gh CLI

latest

Used internally for repo & secret ops

Ansible control repo

v2.15+

Inventory stored in Git, preferably behind PR reviews

(optional) AWX/Tower

21+

REST API token for job‑template cloning

Installation

# Install globally
$ dotnet tool install -g hum

# Or update
$ dotnet tool update -g hum

You’ll need a GitHub PAT exported as HUM_GITHUB_TOKEN or passed via --token.

Quick Start

# Bootstrap a new web API called "orders" and deploy to prod‑eu cluster
hum create orders \
    --template dotnet-webapi \
    --env prod \
    --host app-srv-03 \
    --org my‑company

# After the inventory PR merges, push your real code
cd orders && git push origin main

Within minutes your orders API is live behind NGINX, with nightly SQLite backups and systemd health‑checks.

Commands

Command

Description

hum create <name>

Bootstrap a new project (repo + CI + inventory)

hum list templates

Show available project scaffolds

hum list hosts

Query inventory for eligible app servers

hum doctor

Validate credentials & environment

hum destroy <name>

(Safety first) Remove inventory references and archive repo

Run hum <command> --help for all options.

Configuration

hum searches in this order:

CLI flags (--org, --template, …)

Environment variables (HUM_GITHUB_TOKEN, HUM_DEFAULT_ORG, …)

~/.config/hum/config.yaml

Sample config.yaml:

# ~/.config/hum/config.yaml
org: my-company
inventory_repo: git@github.com:my-company/infra-inventory.git
awx:
  url: https://awx.internal/api/
  token: ${{ env.AWX_TOKEN }}
default_template: dotnet-webapi
default_env: dev

Secrets should not be stored here—use env vars or your secret manager.

How It Works

Template expansion – cookiecutter‑style tokens inject project name & ports into source and workflow files.

GitHub bootstrap – The CLI calls /repos to create, then pushes the scaffold via a detached HEAD.

Inventory PR – New group_vars/<app>.yml and host assignment committed and pushed to a feature branch.

CI pipeline runs on the new repo; once tests pass, it invokes Ansible with inventory path & artifact URL.

Ansible playbooks ensure packages, deploy artifacts, update systemd, and verify health endpoints.

Rollback logic is handled by Ansible: if smoke checks fail, the current symlink re‑points to the previous release and the service restarts.

Roadmap



Check the GitHub Issues for the latest.

Contributing

Pull requests are welcome! Please read CONTRIBUTING.md for setup, coding style, and commit message conventions.

Development setup

# Run the CLI from source
$ dotnet run --project src/hum -- doctor

# Pack & install locally
$ dotnet pack -c Release && dotnet tool install -g --add-source ./nupkg hum --version <version>

License

Code is licensed under the MIT License – see LICENSE.

Logo © 2025 Erik Johnson. Feel free to use it in the context of this project.

