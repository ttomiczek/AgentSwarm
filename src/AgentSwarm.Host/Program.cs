using AgentSwarm.Runtime;

var agentFolder = args.Length > 0 ? args[0] : Directory.GetCurrentDirectory();

var server = AgentSwarmServer.Create(agentFolder);
server.Initialize();
server.Run();