# HoleBite
A very simple, extensible console-based network communication tool.

## Installation
### Developers
Clone the repository and build it yourself
```bash
$ git clone https://github.com/Melodi17/HoleBite
$ cd HoleBite
$ dotnet build
$ dotnet run
```

### Users
Download the latest release from the releases page and run it, or use scoop
```bash
$ scoop install https://raw.githubusercontent.com/Melodi17/HoleBite/refs/heads/master/Deploy/holebite.json
```
Now you can run `holebite` from the command line.

## Protocol

The protocol dictates how each packet is sent between the client and server, and is as follows:

>  Direction is either c2s or s2c, (c2s means client to server, and s2c means server to client)

| Direction | Name | Key | Description |
| --------- | ---- | --- | ----------- |
| `c2s` | Identity declaration | `i'm <identity>` | Provides the server with an identity to associate with the connection |
| `c2s` | Message | `message <content>` | Sends a message to the server containing text (from input bar) |
|  |  |  |  |
| `s2c` | Message | `message <content>` | Sends a message to the client containing text, can be another user's message or a server notification (will be displayed in the output pane) |
| `s2c` | Clear | `clear` | Requests the client to clear their output pane + history |

Each packet is encoded as ASCII to bytes, then the length is calculated and prepended as 32-bit integer:

```
[len:int][data:ascii-byte...]
```



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
