# Pipe-Stream-Communication
Simple example how to use Pipe streams for communication between two applications.
![](PipeClient.gif)
In the example client is able to call the server app, transfering message with parameters. Server calls one of registered methods a passes to it parameters from client and returns results back to client. I use similar implementation to communicate with my background services to change settings or plan tasks for later execution or add them to an queue of tasks.
