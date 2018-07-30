# WinServer
C# HTTP server using asynchronous sockets which allows for local small web hosting. 

## Set Up
#### Make an instance of the **WinServer** class
#### *WinServer* myServer = new *WinServer();*

## Make a Callback Function
###### Once an HTTP request is recieved, a callback function will be invoked
#### myServer.RunServer(*8080*, (req, res) => {
#### *res.Body = "hello world"*;
#### });

## Test it Out!
#### Go to any webbrowser on your machine and type in *127.0.0.1:8080*
#### You should see *hello world* pop up in plain text format
