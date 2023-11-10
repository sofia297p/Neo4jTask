using Neo4j.Driver;
using Neo4jClient;
using Neo4jClient.Cypher;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace MongoTask
{

    internal class SocialNetworkNeo
    {
        private BoltGraphClient clientNeo4j;

        public SocialNetworkNeo()
        {
            clientNeo4j = new BoltGraphClient("neo4j+s://24dec702.databases.neo4j.io", "neo4j", "WgP1WExj8bhqDAFMgx1bvxQMfF-PSJvpshmcVMKbSVI");
            clientNeo4j.ConnectAsync().Wait();
        }
        public void CreateUserNode(string firstName, string lastName, string email)
        {
            PersonNeo newPerson = new PersonNeo { Name = firstName + " " + lastName, Email = email };

            clientNeo4j.Cypher
            .Create("(person:Person $newPerson)")
            .WithParam("newPerson", newPerson)
            .ExecuteWithoutResultsAsync().Wait();
        }
        public void SubscribeTo(string followerEmail, string targetEmail)
        {
            var followExists = clientNeo4j.Cypher
                .Match("(follower:Person)-[:FOLLOWS]->(target:Person)")
                .Where((PersonNeo follower) => follower.Email == followerEmail)
                .AndWhere((PersonNeo target) => target.Email == targetEmail)
                .Return(target => target.As<PersonNeo>())
                .ResultsAsync.Result.Any();


            var subscriberExists = clientNeo4j.Cypher
                .Match("(target:Person)-[:HAS_SUBSCRIBER]->(subscriber:Person)")
                .Where((PersonNeo target) => target.Email == followerEmail)
                .AndWhere((PersonNeo subscriber) => subscriber.Email == targetEmail)
                .Return(subscriber => subscriber.As<PersonNeo>())
                .ResultsAsync.Result.Any();


            if (!followExists)
            {
                clientNeo4j.Cypher
                    .Match("(follower:Person)", "(target:Person)")
                    .Where((PersonNeo follower) => follower.Email == followerEmail)
                    .AndWhere((PersonNeo target) => target.Email == targetEmail)
                    .Create("(follower)-[:FOLLOWS]->(target)")
                    .ExecuteWithoutResultsAsync().Wait();

                clientNeo4j.Cypher
                    .Match("(target:Person)", "(subscriber:Person)")
                    .Where((PersonNeo target) => target.Email == targetEmail)
                    .AndWhere((PersonNeo subscriber) => subscriber.Email == followerEmail)
                    .Create("(target)-[:HAS_SUBSCRIBER]->(subscriber)")
                    .ExecuteWithoutResultsAsync().Wait();
            }


            if (!followExists && subscriberExists)
            {
                clientNeo4j.Cypher
                    .Match("(follower:Person)", "(target:Person)")
                    .Where((PersonNeo follower) => follower.Email == followerEmail)
                    .AndWhere((PersonNeo target) => target.Email == targetEmail)
                    .Create("(follower)-[:IS_FRIENDS_WITH]->(target), (target)-[:IS_FRIENDS_WITH]->(follower)")
                    .ExecuteWithoutResultsAsync().Wait();
            }
        }
        public void UnsubscribeFrom(string followerEmail, string targetEmail)
        {
            var followExists = clientNeo4j.Cypher
           .Match("(follower:Person)-[:FOLLOWS]->(target:Person)")
           .Where((PersonNeo follower) => follower.Email == followerEmail)
           .AndWhere((PersonNeo target) => target.Email == targetEmail)
           .Return(target => target.As<PersonNeo>())
           .ResultsAsync.Result.Any();

         
            if (followExists)
            {
                clientNeo4j.Cypher
                     .Match("(follower:Person)-[r:FOLLOWS]->(target:Person)")
                     .Where((PersonNeo follower) => follower.Email == followerEmail)
                     .AndWhere((PersonNeo target) => target.Email == targetEmail)
                     .Delete("r")
                     .ExecuteWithoutResultsAsync();

                clientNeo4j.Cypher
                  .Match("(target:Person)-[r:HAS_SUBSCRIBER]->(subscriber:Person)")
                  .Where((PersonNeo target) => target.Email == targetEmail)
                  .AndWhere((PersonNeo subscriber) => subscriber.Email == followerEmail)
                  .Delete("r")
                  .ExecuteWithoutResultsAsync();
            }

           

            var areFriends = clientNeo4j.Cypher
                .Match("(follower:Person)-[:IS_FRIENDS_WITH]->(target:Person)")
                .Where((PersonNeo follower) => follower.Email == followerEmail)
                .AndWhere((PersonNeo target) => target.Email == targetEmail)
                .Return(target => target.As<PersonNeo>())
                .ResultsAsync.Result.Any();

            if (areFriends)
            {
                clientNeo4j.Cypher
                    .Match("(follower:Person)-[r:IS_FRIENDS_WITH]->(target:Person)")
                    .Where((PersonNeo follower) => follower.Email == followerEmail)
                    .AndWhere((PersonNeo target) => target.Email == targetEmail)
                    .Delete("r")
                    .ExecuteWithoutResultsAsync();
                clientNeo4j.Cypher
                    .Match("(follower:Person)-[r:IS_FRIENDS_WITH]->(target:Person)")
                    .Where((PersonNeo follower) => follower.Email == targetEmail)
                    .AndWhere((PersonNeo target) => target.Email == followerEmail)
                    .Delete("r")
                    .ExecuteWithoutResultsAsync();
            }
        }
        public string Connection(string userEmail1, string userEmail2)
        {
            var queryResultF = clientNeo4j.Cypher
          .Match("(user1:Person)-[:IS_FRIENDS_WITH]->(user2:Person)")
          .Where((PersonNeo user1) => user1.Email == userEmail1)
          .AndWhere((PersonNeo user2) => user2.Email == userEmail2)
          .Return(user1 => user1.As<PersonNeo>())
          .ResultsAsync.Result.Any();

            var queryResultFl = clientNeo4j.Cypher
          .Match("(user1:Person)-[:FOLLOWS]->(user2:Person)")
          .Where((PersonNeo user1) => user1.Email == userEmail1)
          .AndWhere((PersonNeo user2) => user2.Email == userEmail2)
          .Return(user1 => user1.As<PersonNeo>())
          .ResultsAsync.Result.Any();

            var queryResultS = clientNeo4j.Cypher
          .Match("(user1:Person)-[:HAS_SUBSCRIBER]->(user2:Person)")
          .Where((PersonNeo user1) => user1.Email == userEmail1)
          .AndWhere((PersonNeo user2) => user2.Email == userEmail2)
          .Return(user1 => user1.As<PersonNeo>())
          .ResultsAsync.Result.Any();

            if (queryResultF)
            {
                return "You are friends";
            }
            else if (queryResultFl && !queryResultF)
            {
                return "You are following this person";

            }
            else if (queryResultS)
            {
                return "This person follows you";
            }
            return "No connection";

        }
        public int GetDistanceBetweenUsers(string userEmail1, string userEmail2)
        {
            var queryResult = clientNeo4j.Cypher
          .Match("p = shortestPath((user1:Person)-[:IS_FRIENDS_WITH*]-(user2:Person))")
          .Where((PersonNeo user1) => user1.Email == userEmail1)
          .AndWhere((PersonNeo user2) => user2.Email == userEmail2)
          .Return(p => Return.As<IEnumerable<string>>("nodes(p)"))
          .ResultsAsync.Result;
            if (queryResult.Any())
            {
                var nodesInPath = queryResult.First();
                var uniqueNodes = nodesInPath.Distinct().ToList();
                var pathLength = uniqueNodes.Count() - 1;
                return pathLength;
            }
            
            return -1;
            }
            
        

        public void DeleteUser(string userEmail)
        {
            clientNeo4j.Cypher
            .Match("(p:Person {email: $personEmail})")
            .WithParam("personEmail", userEmail)
            .DetachDelete("p")
            .ExecuteWithoutResultsAsync().Wait();
        }

    }
}

    
        

