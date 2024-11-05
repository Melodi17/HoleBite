# HoleBite
A very simple, extensible console-based network communication tool.

## Protocol

The protocol dictates how each packet is sent between the client and server, and is as follows:

>  Direction is either c2s or s2c, (c2s means client to server, and s2c means server to client)

| Direction | Name | Key | Description |
| --------- | ---- | --- | ----------- |
| `c2s` | Identity declaration | `i'm <identity>` | Provides the server with an identity to associate with the connection |
| `c2s` | Message | `message <content>` | Sends a message to the server containing text (from input bar) |
|  |  |  |  |
| `s2c` | Message | `message <content>` | Sends a message to the client containing text, can be another user's message or a server notification (will be displayed in the output pane) |
| `s2c` | Format change | `rich <format>` | Sends a format instruction to the client, which can be any `ConsoleColor` |
| `s2c` | Clear | `clear` | Requests the client to clear their output pane + history |

\* The `bye` command is also included, and can be sent from either side, either forcing the client to close, or telling the server that the client is closing.

## Parts

### The server

```bash
$ holebite --server
```



The default server is responsible for handling all the clients that connect to it, and places each one on its own thread, it will not allow any messages through until the client has shared it's identity. After that, all messages sent to it will be broadcast to all connected devices.

### The client

`````bash
$ holebite <server> <name>
`````



The default client includes two parts, the input bar down the bottom and an output pane that takes up the rest of the window. Once connected to the server, it sends an identity packet of the name from the startup. It will then display all new message packets **from the server**, and typing and pressing enter in the input pane will send a message packet **to the server**.
