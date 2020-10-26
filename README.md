--------------------------------------------------
---- ChatSystem using a Tcp connection ----
--------------------------------------------------

This WPF based chat program makes use of my TcpConnector ( [TcpConnector](https://github.com/Kiminaze/TcpConnector) ) to create a connection between a server and several clients. This is mainly just a showcase of how to use the TcpConnector.

**Server Features:**
- You can set a custom port (port needs to be forwarded in your firewall)
- starting / stopping server
- shows all messages in history

**Client Features:**
- set custom name
- specify IP address and port of the server
- connect to / disconnect from a server
- send messages
- view message history
