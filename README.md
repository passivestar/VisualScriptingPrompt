# Visual Scripting Prompt



https://user-images.githubusercontent.com/60579014/177394007-7a95e094-70d1-41fa-985d-8acce53b8f94.mov



Visual scripting command prompt for better efficiency. Press Shift+Q to open. Early work in progress, use at your own risk

Compatible with Unity `2022.1.x` and Visual Scripting `1.7.8`

Edit `config.json` to add your own unit and command aliases

## Features:

- Chains nodes for you and connects compatible ports
- Shortcuts allow you to alias any node signature to a short string (for example 1,2,3,4 for Float, Vector2, Vector3 and Quaternion)
- You can easily add subgraphs and define any control/value inputs and outputs in one line without having to open the side panel
- You can create variables of any scope (graph, object, scene, app, saved) without having to open the side panel and autoattach the getter/setter
- You can traverse through the stack of nodes that you've written to write more complex one-liners
- It has a very flexible implementation internally. All of the features above (units, subgraphs, ports, variables, traversing) are implemented as commands. New commands can be added easily

## Available commands:

- `[name]` - Add a node with the given `name`
- `![name]` - Add a node with the given `name` to the left of the current node

- `sg:[name]` - Add a subgraph with name `name`
- `sgin:[name]` - Add a subgraph with name `name` with just the input control
- `sgout:[name]` - Add a subgraph with name `name` with just the output control

- `vi:[name]` - Add value input with name `name`
- `vo:[name]` - Add value output with name `name`
- `ci:[name]` - Add control input with name `name`
- `co:[name]` - Add control output with name `name`

- `g:[name]` - Get graph variable with name `name`
- `setg:[name]` - Set graph variable with name `name`
- `o:[name]` - Get object variable with name `name`
- `seto:[name]` - Set object variable with name `name`
- `s:[name]` - Get scene variable with name `name`
- `sets:[name]` - Set scene variable with name `name`
- `a:[name]` - Get app variable with name `name`
- `seta:[name]` - Set app variable with name `name`
- `sv:[name]` - Get saved variable with name `name`
- `setsv:[name]` - Set saved variable with name `name`

- `^[number]` - Go up the chain `number` amount of times

## Known Issues:

- Object variables don't work properly
