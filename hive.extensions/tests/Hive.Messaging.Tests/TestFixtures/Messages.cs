namespace Hive.Messaging.Tests.TestFixtures;

public record TestMessage(string Content);

public record TestRequest(string Query);

public record TestResponse(string Result);
