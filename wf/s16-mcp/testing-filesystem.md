
Используй mcp filesystem  и перечисли файлы в каталоге E:\vibe-projects\3.4pr\instructor\docs\cli


- batch.md
- finetune.md
- index.md
- usage.md

-------------------------------------------

Используй mcp filesystem  и прочитай содержимое файла index.md


Содержимое `index.md`:

```markdown
---
title: Instructor CLI Tools
description: Command-line utilities for monitoring API usage, fine-tuning models, and accessing documentation.
---

# Instructor CLI Tools

<div class="grid cards" markdown>

- :material-console: **Command Line Utilities**
    Powerful tools to enhance your Instructor workflow
    [:octicons-arrow-right-16: View Commands](#available-commands)

- :material-chart-line: **Usage Monitoring**
    Track API usage, costs, and token consumption
    [:octicons-arrow-right-16: Usage Guide](usage.md)

- :material-tune-vertical: **Model Fine-Tuning**
    Create and manage custom model versions
    [:octicons-arrow-right-16: Fine-Tuning Guide](finetune.md)

- :material-book-open-variant: **Documentation Access**
    Quickly access docs from your terminal
    [:octicons-arrow-right-16: Docs Command](#documentation-command)

</div>

## Getting Started

### Installation
```bash
pip install instructor
```

### API Setup
```bash
export OPENAI_API_KEY="your-api-key-here"
```

## Available Commands

| Command | Description | Guide |
|---------|-------------|-------|
| `instructor usage` | Track API usage and costs | [Usage Guide](usage.md) |
| `instructor finetune` | Create and manage fine-tuned models | [Fine-Tuning Guide](finetune.md) |
| `instructor docs` | Quick access to documentation | [See below](#documentation-command) |

## Usage Command
```bash
instructor usage
instructor usage --by-day
instructor usage --model gpt-4
```

## Fine-Tuning Command
```bash
instructor finetune
```

## Documentation Command
```bash
instructor docs
instructor docs validation
instructor docs concepts/models
```
```