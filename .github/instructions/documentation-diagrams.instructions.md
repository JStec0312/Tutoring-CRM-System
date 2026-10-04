---
description: Standards for technical documentation and UML diagrams
applyTo: 'docs/**'
---

# Technical documentation diagrams

- Documentation should almost always include a UML diagram. Prefer a sequence diagram for API and use-case flows.
- Keep editable PlantUML source in a `diagrams/` directory next to the use-case documentation, and embed the rendered SVG in its README with a link to the `.puml` source.
- Make diagrams reflect the verified implementation; omit them only when they add no meaningful clarity or the user explicitly requests it.
