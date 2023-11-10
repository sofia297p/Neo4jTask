using System;
using System.Linq;
using System.Text;
using Microsoft.Extensions.Configuration;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using Neo4jClient;
using Neo4jClient.Cypher;
using Neo4j.Driver;
using Newtonsoft.Json;


namespace MongoTask
{
    
    public class Person
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }


        [BsonElement("E-mail")]
        public string Email { get; set; }


        [BsonElement("Password")]
        public string Password { get; set; }

        [BsonElement("FirstName")]
        public string FirstName { get; set; }

        [BsonElement("LastName")]
        public string LastName { get; set; }

        [BsonElement("Interests")]
        public List<string> Interests { get; set; }

        [BsonElement("Friends")]
        public List<string> Friends { get; set; }

        [BsonElement("Following")]
        public List<string> Following { get; set; }

        [BsonElement("Subscribers")]
        public List<string> Subscribers { get; set; }

        [BsonElement("posts")]
        public List<Post> Posts { get; set; }
    }
    public class Post
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        [BsonElement("title")]
        public string Title { get; set; }

        [BsonElement("body")]
        public string PostBody { get; set; }

        [BsonElement("date")]
        public DateTime Date { get; set; }

        [BsonElement("author")]
        public string AuthorId { get; set; }

        [BsonElement("likesCount")]
        public int LikesCount { get; set; }

        [BsonElement("likes")]
        public List<string> Likes { get; set; }


        [BsonElement("comments")]
        public List<Comment> Comments { get; set; }


    }

    public class Comment
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        [BsonElement("body")]
        public string CommentBody { get; set; }

        [BsonElement("date")]
        public DateTime Date { get; set; }

        [BsonElement("author")]
        public string AuthorID { get; set; }

        [BsonElement("likesCount")]
        public int LikesCount { get; set; }

        [BsonElement("likes")]
        public List<string> LikesId { get; set; }

        [BsonElement("postId")] 
        public string PostId { get; set; }
    }
    public class PersonNeo
    {
        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }

        [JsonProperty(PropertyName = "email")]
        public string Email { get; set; }

    }
    internal class Program
    {

        static void Main(string[] args)
        {
            string connectionString = "mongodb://localhost:27017";
            var socialNetwork = new SocialNetwork(connectionString, "Test");
            var socialNetworkNeo = new SocialNetworkNeo();
            Person loggedInUser=null;

            while (true)
            {
                Console.WriteLine("*****Social Network Menu*****");
                Console.WriteLine("1. Log in");
                Console.WriteLine("2. Show stream");
                Console.WriteLine("3. Subscribe to a user");
                Console.WriteLine("4. Unsubscribe from a user");
                Console.WriteLine("5. Create a post");
                Console.WriteLine("6. Create a comment");
                Console.WriteLine("7. Add like to post");
                Console.WriteLine("8. Remove like from post");
                Console.WriteLine("9. Add like to comment");
                Console.WriteLine("10. Remove like from comment");
                Console.WriteLine("11.Find user with email");
                Console.WriteLine("12.See all posts from user");
                Console.WriteLine("13.Create user");
                Console.WriteLine("14.Delete user");
                Console.WriteLine("15. Exit");
                
                Console.Write("Select an option: ");

                string choice = Console.ReadLine();
                switch (choice)
                {
                    case "1":
                        Console.Write("Enter your email: ");
                        string email = Console.ReadLine();
                        Console.Write("Enter your password: ");
                        string password = Console.ReadLine();
                        try {
                            loggedInUser = socialNetwork.Login(email, password);
                            Console.WriteLine();
                            Console.WriteLine($"Logged in as :{loggedInUser.FirstName} {loggedInUser.LastName}");
                            Console.WriteLine();
                            List<Post> posts = socialNetwork.GetPostsFromFollowedUsers(loggedInUser);
                            socialNetwork.PrintPostsInfo(posts,socialNetwork);
                        }
                        catch (Exception ex) { Console.WriteLine(ex.Message); }
                       
                        break;

                    case "2":
                        if (loggedInUser != null)
                        {
                          var followedUsersPosts = socialNetwork.GetPostsFromFollowedUsers(loggedInUser);

                            if (followedUsersPosts.Count > 0)
                            {
                                Console.WriteLine("Posts from users you are following:");
                                socialNetwork.PrintPostsInfo(followedUsersPosts, socialNetwork);
                            }
                            else
                            {
                                Console.WriteLine("You are not following anyone, or the users you follow haven't posted yet.");
                            }
                        }
                        else
                        {
                            Console.WriteLine("You need to log in first.");
                        }
                        break;

                    case "3":
                        if (loggedInUser != null)
                        {
                            Console.Write("Enter the email of the user you want to subscribe to: ");
                            string targetEmail = Console.ReadLine();

                            Person userToSubscribe = socialNetwork.FindPerson(targetEmail, socialNetwork.GetUsers());
                            if (userToSubscribe != null)
                            {
                                socialNetwork.SubscribeTo(loggedInUser, userToSubscribe);

                                var followerEm = loggedInUser.Email;
                                socialNetworkNeo.SubscribeTo(followerEm, targetEmail);
                                Console.WriteLine($"You have subscribed to {userToSubscribe.FirstName} {userToSubscribe.LastName}");
                            }
                            else
                            {
                                Console.WriteLine("User not found. Please try again.");
                            }
                        }
                        else
                        {
                            Console.WriteLine("You need to log in first.");
                        }
                        break;
                        
                       

                    case "4":
                        if (loggedInUser != null)
                        {
                            Console.Write("Enter the email of the user you want to unsubscribe from: ");
                            string targetEmail = Console.ReadLine();

                            Person userToUnsubscribe = socialNetwork.FindPerson(targetEmail, socialNetwork.GetUsers());
                            if (userToUnsubscribe != null)
                            {
                                socialNetwork.UnsubscribeFrom(loggedInUser, userToUnsubscribe);

                                var followerEm = loggedInUser.Email;
                                socialNetworkNeo.UnsubscribeFrom(followerEm, targetEmail);
                                Console.WriteLine($"You have unsubscribed from {userToUnsubscribe.FirstName} {userToUnsubscribe.LastName}");
                            }
                            else
                            {
                                Console.WriteLine("User not found. Please try again.");
                            }
                        }
                        else
                        {
                            Console.WriteLine("You need to log in first.");
                        }
                       
                        break;

                    case "5":
                        if (loggedInUser != null)
                        {
                            Console.Write("Enter post title: ");
                            string postTitle = Console.ReadLine();
                            Console.WriteLine("Enter post body (enter an empty line to finish):");

                            StringBuilder stringBuilder = new StringBuilder();
                            string currentRow;

                            while ((currentRow = Console.ReadLine()) != "")
                                stringBuilder.AppendLine(currentRow);

                            string postBody = stringBuilder.ToString();


                            socialNetwork.CreatePost(loggedInUser, postTitle, postBody);
                            Console.WriteLine("Post created successfully!");
                        }
                        else
                        {
                            Console.WriteLine("You need to log in first.");
                        }
                        break;
                      

                    case "6":
                        if (loggedInUser != null)
                        {
                            Console.Write("Enter comment body (enter an empty line to finish):");

                            StringBuilder stringBuilder = new StringBuilder();
                            string currentRow;

                            while ((currentRow = Console.ReadLine()) != "")
                                stringBuilder.AppendLine(currentRow);

                            string commentBody = stringBuilder.ToString();
                            Console.Write("Enter post ID for the comment: ");
                            string postId = Console.ReadLine();

                            Post post = socialNetwork.GetPostById(postId);

                            if (post != null)
                            {
                                socialNetwork.CreateComment(loggedInUser, commentBody, post);
                                Console.WriteLine("Comment created successfully!");
                            }
                            else
                            {
                                Console.WriteLine("Post not found. Please check the post ID.");
                            }
                        }
                        else
                        {
                            Console.WriteLine("You need to log in first.");
                        }
                        break;

                    case "7":
                        if (loggedInUser != null)
                        {
                            Console.Write("Enter the post ID you want to like: ");
                            string postId = Console.ReadLine();
                            Post postToLike = socialNetwork.GetPostById(postId);

                            if (postToLike != null)
                            {
                                socialNetwork.AddLikeToPost(loggedInUser, postId);
                                Console.WriteLine("You liked the post!");
                            }
                            else
                            {
                                Console.WriteLine("Post not found. Please enter a valid post ID.");
                            }
                        }
                        else
                        {
                            Console.WriteLine("You need to log in first.");
                        }
                        break;
                    case "8":
                        if (loggedInUser != null)
                        {
                            Console.Write("Enter the post ID you want to remove your like from: ");
                            string postId = Console.ReadLine();
                            Post postToUnlike = socialNetwork.GetPostById(postId);

                            if (postToUnlike != null)
                            {
                                socialNetwork.RemoveLikeFromPost(loggedInUser, postId);
                                Console.WriteLine("You removed your like from the post.");
                            }
                            else
                            {
                                Console.WriteLine("Post not found. Please enter a valid post ID.");
                            }
                        }
                        else
                        {
                            Console.WriteLine("You need to log in first.");
                        }
                        break;
                    case "9":
                        if (loggedInUser != null)
                        {
                            Console.Write("Enter the comment ID you want to like: ");
                            string commentId = Console.ReadLine();
                            Comment commentToLike = socialNetwork.GetCommentById(commentId);

                            if (commentToLike != null)
                            {
                                socialNetwork.AddLikeToComment(loggedInUser, commentId);
                                Console.WriteLine("You liked the comment!");
                            }
                            else
                            {
                                Console.WriteLine("Comment not found. Please enter a valid comment ID.");
                            }
                        }
                        else
                        {
                            Console.WriteLine("You need to log in first.");
                        }
                        break;
                    case "10":
                        if (loggedInUser != null)
                        {
                            Console.Write("Enter the comment ID you want to remove your like from: ");
                            string commentId = Console.ReadLine();
                            Comment commentToUnlike = socialNetwork.GetCommentById(commentId);

                            if (commentToUnlike != null)
                            {
                                socialNetwork.RemoveLikeFromComment(loggedInUser, commentId);
                                Console.WriteLine("You removed your like from the comment.");
                            }
                            else
                            {
                                Console.WriteLine("Comment not found. Please enter a valid comment ID.");
                            }
                        }
                        else
                        {
                            Console.WriteLine("You need to log in first.");
                        }
                        break;
                    case "11":
                        if (loggedInUser != null)
                        {
                            Console.Write("Enter the email of the user you want to search for: ");
                            string targetEmail = Console.ReadLine();

                            
                            List<Person> users = socialNetwork.GetUsers();
                            Person targetUser = socialNetwork.FindPerson(targetEmail, users);

                            if (targetUser != null)
                            {
                                Console.WriteLine($"User found: {targetUser.FirstName} {targetUser.LastName} ({targetUser.Email})");
                                Console.WriteLine("Interests:"+"\n");
                                foreach (var el in targetUser.Interests)
                                {
                                    Console.WriteLine(el);
                                }
                                Console.WriteLine();

                                Console.WriteLine(socialNetworkNeo.Connection(loggedInUser.Email, targetUser.Email));
                                Console.WriteLine();
                                var path = socialNetworkNeo.GetDistanceBetweenUsers(loggedInUser.Email, targetUser.Email);
                                if ( path!= -1)
                                {
                                    Console.WriteLine($"Shortest path: {path}");
                                }
                            }
                            else
                            {
                                Console.WriteLine("User not found.");
                            }
                        }
                        else
                        {
                            Console.WriteLine("You need to log in first.");
                        }
                        break;
                    case "12":
                        if (loggedInUser != null)
                        {
                            Console.Write("Enter the email of the user whose posts you want to view: ");
                            string targetUserEmail = Console.ReadLine();

                            Console.WriteLine();
                            Person targetUser = socialNetwork.FindPerson(targetUserEmail, socialNetwork.GetUsers());

                            if (targetUser != null)
                            {
                                List<Post> posts = socialNetwork.GetPostsForUser(targetUser);
                                socialNetwork.PrintPostsInfo(posts, socialNetwork);
                            }
                            else
                            {
                                Console.WriteLine("User not found.");
                            }
                        }
                        else
                        {
                            Console.WriteLine("You need to log in first.");
                        }
                        break;
                    case "13":
                        
                        Console.Write("Enter user email: ");
                        string newUserEmail = Console.ReadLine();
                        Console.Write("Enter user password: ");
                        string newUserPassword = Console.ReadLine();
                        Console.Write("Enter user first name: ");
                        string newUserFirstName = Console.ReadLine();
                        Console.Write("Enter user last name: ");
                        string newUserLastName = Console.ReadLine();

                        try
                        {
                            socialNetwork.CreateUser(newUserEmail, newUserPassword, newUserFirstName, newUserLastName);
                            socialNetworkNeo.CreateUserNode(newUserFirstName, newUserLastName, newUserEmail);
                            Console.WriteLine("User created successfully!");
                        }
                        catch (ArgumentException ex)
                        {
                            Console.WriteLine("Error: " + ex.Message);
                        }
                        break;

                    case "14":
                        Console.Write("Enter user email to delete: ");
                        string userEmailToDelete = Console.ReadLine();

                        try
                        {
                            socialNetwork.DeleteUser(userEmailToDelete);

                            socialNetworkNeo.DeleteUser(userEmailToDelete);
                            Console.WriteLine("User deleted successfully!");
                        }
                        catch (InvalidOperationException ex)
                        {
                            Console.WriteLine("Error: " + ex.Message);
                        }
                        break;

                    case "15":
                        return;


                    default:
                        Console.WriteLine("Invalid choice. Please select a valid option.");
                        break;
                }
            }
        }





        

        }

    }
