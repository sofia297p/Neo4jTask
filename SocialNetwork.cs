using MongoDB.Driver;
using MongoTask;
using System;
using System.Linq;

public class SocialNetwork
{
    private IMongoCollection<Person> collection;
    private IMongoDatabase database;

    

    public SocialNetwork(string connectionString, string databaseName)
	{
       var client = new MongoClient(connectionString);
       database = client.GetDatabase(databaseName);
       collection = database.GetCollection<Person>("people");
    }
    public Person CreateUser(string email, string password, string firstName, string lastName)
    {
        
        if (collection.AsQueryable().Any(u => u.Email == email))
        {
            throw new ArgumentException("User with this email already exists");
        }

        
        Person newUser = new Person
        {
            Email = email,
            Password = password,
            FirstName = firstName,
            LastName = lastName,
            Interests = new List<string>(),
            Friends = new List<string>(),
            Following = new List<string>(),
            Subscribers = new List<string>(),
            Posts = new List<Post>()
        };

        collection.InsertOne(newUser);
        return newUser;
    }
    public void DeleteUser(string userEmail)
    {
        
        var userToDelete = collection.Find(u => u.Email == userEmail).SingleOrDefault();

        if (userToDelete == null)
        {
            throw new InvalidOperationException("User not found");
        }

        collection.DeleteOne(u => u.Email == userEmail);

        foreach (var friendId in userToDelete.Friends)
        {
            var friend = GetUserById(friendId);
            friend.Friends.Remove(userToDelete.Id);
            var friendUpdate = Builders<Person>.Update.Set("Friends", friend.Friends);
            collection.UpdateOne(p => p.Id == friend.Id, friendUpdate);
        }

        foreach (var subscriberId in userToDelete.Subscribers)
        {
            var subscriber = GetUserById(subscriberId);
            subscriber.Following.Remove(userToDelete.Id);
            var subscriberUpdate = Builders<Person>.Update.Set("Following", subscriber.Following);
            collection.UpdateOne(p => p.Id == subscriber.Id, subscriberUpdate);
        }
        foreach (var followingId in userToDelete.Following)
        {
            var followingUser = GetUserById(followingId);
            followingUser.Subscribers.Remove(userToDelete.Id);
            var followingUserUpdate = Builders<Person>.Update.Set("Subscribers", followingUser.Subscribers);
            collection.UpdateOne(p => p.Id == followingUser.Id, followingUserUpdate);
        }
    }
    public Person Login(string email, string password)
    {
        var user = collection.Find(u => u.Email == email && u.Password == password).SingleOrDefault();

        if (user != null)
        {
            return user; 
        }
        else
        {
          throw new ArgumentException("Incorrect email or password\n");
            
        }
    }
    public void CreatePost(Person author, string title, string postBody)
    {

        Post newPost = new Post
        {
            Title = title,
            PostBody = postBody,
            Date = DateTime.Now,
            AuthorId = author.Id,
            LikesCount = 0, 
            Likes = new List<string>(), 
            Comments = new List<Comment>() 
        };

        
        var postsCollection = database.GetCollection<Post>("posts");
        postsCollection.InsertOne(newPost);

       
        author.Posts.Add(newPost);
        var authorUpdate = Builders<Person>.Update.Set("posts", author.Posts);
        collection.UpdateOne(p => p.Id == author.Id, authorUpdate);
    }

    public void CreateComment(Person author, string commentBody, Post post)
    {
        Comment newComment = new Comment
        {
            CommentBody = commentBody,
            Date = DateTime.Now,
            AuthorID = author.Id,
            LikesCount = 0,
            LikesId = new List<string>(),
            PostId= post.Id
        };

        var commentsCollection = database.GetCollection<Comment>("Comments");
        commentsCollection.InsertOne(newComment);

       
        post.Comments.Add(newComment);

        


        var postUpdate = Builders<Post>.Update.Set("Comments", post.Comments);
        var postsCollection = database.GetCollection<Post>("posts");
        postsCollection.UpdateOne(p => p.Id == post.Id, postUpdate);


        var authorOfPost = GetUserById(post.AuthorId);
        int postIndex = authorOfPost.Posts.FindIndex(p => p.Id == post.Id);
        authorOfPost.Posts[postIndex] = post;
        var authorUpdate = Builders<Person>.Update.Set("posts", authorOfPost.Posts);
        collection.UpdateOne(p => p.Id == authorOfPost.Id, authorUpdate);
    }
    public void AddLikeToPost(Person person, string postId)
    {
        var postsCollection = database.GetCollection<Post>("posts");
       
        var post = postsCollection.Find(p => p.Id == postId).SingleOrDefault();



        if (!post.Likes.Contains(person.Id))
        {
            post.LikesCount++;


            post.Likes.Add(person.Id);

            var postUpdateL = Builders<Post>.Update.Inc("likesCount",1);

            postsCollection.UpdateOne(p => p.Id == post.Id, postUpdateL);

            var postUpdate = Builders<Post>.Update.Set("likes", post.Likes);

            postsCollection.UpdateOne(p => p.Id == post.Id, postUpdate);

            var authorOfPost = GetUserById(post.AuthorId);
            int postIndex = authorOfPost.Posts.FindIndex(p => p.Id == post.Id);
            authorOfPost.Posts[postIndex] = post;
            var authorUpdate = Builders<Person>.Update.Set("posts", authorOfPost.Posts);
            collection.UpdateOne(p => p.Id == authorOfPost.Id, authorUpdate);

        }
        
    }
    public void RemoveLikeFromPost(Person person, string postId)
    {
        var postsCollection = database.GetCollection<Post>("posts");

        var post = postsCollection.Find(p => p.Id == postId).SingleOrDefault();

        if (post.Likes.Contains(person.Id))
        {
            post.LikesCount--;

            post.Likes.Remove(person.Id);

            var postUpdateL = Builders<Post>.Update.Inc("likesCount", -1);
            postsCollection.UpdateOne(p => p.Id == post.Id, postUpdateL);

            var postUpdate = Builders<Post>.Update.Set("likes", post.Likes);
            postsCollection.UpdateOne(p => p.Id == post.Id, postUpdate);

            var authorOfPost = GetUserById(post.AuthorId);
            int postIndex = authorOfPost.Posts.FindIndex(p => p.Id == post.Id);
            authorOfPost.Posts[postIndex] = post;
            var authorUpdate = Builders<Person>.Update.Set("posts", authorOfPost.Posts);
            collection.UpdateOne(p => p.Id == authorOfPost.Id, authorUpdate);
        }
    }
    public void AddLikeToComment(Person person, string commentId)
    {
        var commentsCollection = database.GetCollection<Comment>("Comments");
        var comment = commentsCollection.Find(c => c.Id == commentId).SingleOrDefault();
        var postsCollection = database.GetCollection<Post>("posts");

        if (!comment.LikesId.Contains(person.Id))
        {

            comment.LikesCount++;

            comment.LikesId.Add(person.Id);

            var commentUpdateL = Builders<Comment>.Update.Inc("likesCount", 1);
            commentsCollection.UpdateOne(c => c.Id == commentId, commentUpdateL);

            var commentUpdate = Builders<Comment>.Update.Set("likes", comment.LikesId);
            commentsCollection.UpdateOne(c => c.Id == commentId, commentUpdate);

            var postWhereComment=GetPostById(comment.PostId);
            int commentindex=postWhereComment.Comments.FindIndex(c=>c.Id == comment.Id);
            postWhereComment.Comments[commentindex] = comment;
            var postUpdate = Builders<Post>.Update.Set("comments", postWhereComment.Comments);
            postsCollection.UpdateOne(p => p.Id == postWhereComment.Id, postUpdate);

            var authorOfPost = GetUserById(postWhereComment.AuthorId);
            int postIndex = authorOfPost.Posts.FindIndex(p => p.Id == postWhereComment.Id);
            authorOfPost.Posts[postIndex] = postWhereComment;
            var authorUpdate = Builders<Person>.Update.Set("posts", authorOfPost.Posts);
            collection.UpdateOne(p => p.Id == authorOfPost.Id, authorUpdate);
        }
        
        
    }
    public void RemoveLikeFromComment(Person person, string commentId)
    {
        var commentsCollection = database.GetCollection<Comment>("Comments");
        var comment = commentsCollection.Find(c => c.Id == commentId).SingleOrDefault();
        var postsCollection = database.GetCollection<Post>("posts");

        if (comment.LikesId.Contains(person.Id))
        {
            comment.LikesCount--;

            comment.LikesId.Remove(person.Id);

            var commentUpdateL = Builders<Comment>.Update.Inc("likesCount", -1);
            commentsCollection.UpdateOne(c => c.Id == commentId, commentUpdateL);

            var commentUpdate = Builders<Comment>.Update.Set("likes", comment.LikesId);
            commentsCollection.UpdateOne(c => c.Id == commentId, commentUpdate);

            var postWhereComment = GetPostById(comment.PostId);
            int commentIndex = postWhereComment.Comments.FindIndex(c => c.Id == comment.Id);
            postWhereComment.Comments[commentIndex] = comment;
            var postUpdate = Builders<Post>.Update.Set("comments", postWhereComment.Comments);
            postsCollection.UpdateOne(p => p.Id == postWhereComment.Id, postUpdate);

            var authorOfPost = GetUserById(postWhereComment.AuthorId);
            int postIndex = authorOfPost.Posts.FindIndex(p => p.Id == postWhereComment.Id);
            authorOfPost.Posts[postIndex] = postWhereComment;
            var authorUpdate = Builders<Person>.Update.Set("posts", authorOfPost.Posts);
            collection.UpdateOne(p => p.Id == authorOfPost.Id, authorUpdate);
        }
    }
    public void SubscribeTo(Person subscriber, Person userToSubscribe)
    {
       

       
        if (!subscriber.Following.Contains(userToSubscribe.Id))
        {
            subscriber.Following.Add(userToSubscribe.Id);

            var subscriberUpdateDefinition = Builders<Person>.Update.Set("Following", subscriber.Following);
            collection.UpdateOne(p => p.Id == subscriber.Id, subscriberUpdateDefinition);
        }

       

        if (!userToSubscribe.Subscribers.Contains(subscriber.Id))
        {
            userToSubscribe.Subscribers.Add(subscriber.Id);

           var userToSubscribeUpdate = Builders<Person>.Update.Set("Subscribers", userToSubscribe.Subscribers);

            collection.UpdateOne(p => p.Id == userToSubscribe.Id, userToSubscribeUpdate);
        }

        if (subscriber.Following.Contains(userToSubscribe.Id) && userToSubscribe.Following.Contains(subscriber.Id)&& !subscriber.Following.Contains(userToSubscribe.Id))
        {
            subscriber.Friends.Add(userToSubscribe.Id);

            userToSubscribe.Friends.Add(subscriber.Id);

            var subscriberUpdateDefinition = Builders<Person>.Update.Set("Friends", subscriber.Friends);
            collection.UpdateOne(p => p.Id == subscriber.Id, subscriberUpdateDefinition);

            var userToSubscribeUpdate = Builders<Person>.Update.Set("Friends", userToSubscribe.Friends);

            collection.UpdateOne(p => p.Id == userToSubscribe.Id, userToSubscribeUpdate);

        }

    }
    public void UnsubscribeFrom(Person subscriber, Person userToUnsubscribe)
    {
        if (subscriber.Following.Contains(userToUnsubscribe.Id))
        {
            subscriber.Following.Remove(userToUnsubscribe.Id);
            var subscriberUpdateDefinition = Builders<Person>.Update.Set("Following", subscriber.Following);
            collection.UpdateOne(p => p.Id == subscriber.Id, subscriberUpdateDefinition);
            if (subscriber.Friends.Contains(userToUnsubscribe.Id))
            {
                subscriber.Friends.Remove(userToUnsubscribe.Id);
                userToUnsubscribe.Friends.Remove(subscriber.Id);

                var subscriberUpdateDef = Builders<Person>.Update.Set("Friends", subscriber.Friends);
                collection.UpdateOne(p => p.Id == subscriber.Id, subscriberUpdateDef);

                var userToUnsubscribeUpdate = Builders<Person>.Update.Set("Friends", userToUnsubscribe.Friends);
                collection.UpdateOne(p => p.Id == userToUnsubscribe.Id, userToUnsubscribeUpdate);
            }
        }

        if (userToUnsubscribe.Subscribers.Contains(subscriber.Id))
        {
            userToUnsubscribe.Subscribers.Remove(subscriber.Id);
            var userToUnsubscribeUpdate = Builders<Person>.Update.Set("Subscribers", userToUnsubscribe.Subscribers);
            collection.UpdateOne(p => p.Id == userToUnsubscribe.Id, userToUnsubscribeUpdate);
        }


    }
   
    public Person FindPerson(string targetEmail,List<Person> users)
    {
       Person userWithMatchingEmail;
       return userWithMatchingEmail = users.SingleOrDefault(user => user.Email == targetEmail);
    }
    public List<Post> GetPostsForUser(Person user)
    {
        return user.Posts;
    }


    
    public List<Person> GetUsers()
    {
        var filter = Builders<Person>.Filter.Empty;
        var projection = Builders<Person>.Projection.Exclude("Password");


        var users = collection.Find(filter).Project<Person>(projection).ToList();

        return users;
    }
    public Person GetUserById(string userId)
    {
       var filter = Builders<Person>.Filter.Eq(p => p.Id, userId);
       var user = collection.Find(filter).SingleOrDefault();
       return user;
    }
    public Post GetPostById(string postId)
    {

        var postsCollection = database.GetCollection<Post>("posts");
        var filter = Builders<Post>.Filter.Eq(p => p.Id, postId);


        var post = postsCollection.Find(filter).SingleOrDefault();

        return post;
    }
    public Comment GetCommentById(string commentId)
    {
        var commentsCollection = database.GetCollection<Comment>("Comments");
        var filter = Builders<Comment>.Filter.Eq(c => c.Id, commentId);
        var comment = commentsCollection.Find(filter).SingleOrDefault();
        return comment;
    }
    public List<Post> GetPostsFromFollowedUsers(Person loggedInUser)
    {
        var followedUserIds = loggedInUser.Following;
        var postsCollection = database.GetCollection<Post>("posts");

        
        var filter = Builders<Post>.Filter.In(p => p.AuthorId, followedUserIds);

        
        var sortDefinition = Builders<Post>.Sort.Descending(p => p.Date);

        
        var posts = postsCollection.Find(filter).Sort(sortDefinition).ToList();

        return posts;
    }
    public void PrintPostsInfo(List<Post> posts, SocialNetwork socialNetwork)
    {
        foreach (var post in posts)
        {
            Console.WriteLine($"Post Id: {post.Id}");
            Console.WriteLine($"Title: {post.Title}");
            Console.WriteLine(post.PostBody);
            Console.WriteLine($"Date: {post.Date}");
            var author = socialNetwork.GetUserById(post.AuthorId);

            Console.WriteLine($"Author: {author.FirstName} {author.LastName}");
            Console.WriteLine($"Likes: {post.LikesCount}");
            Console.WriteLine("People who liked:");

            foreach (var likeId in post.Likes)
            {
                var user = socialNetwork.GetUserById(likeId);
                Console.WriteLine($" - {user.FirstName} {user.LastName}");
            }

            Console.WriteLine("Comments:");
            foreach (var comment in post.Comments)
            {
                Console.WriteLine($"   Comment Id: {comment.Id}");
                Console.WriteLine($"   Comment Text: {comment.CommentBody}");
                Console.WriteLine($"   Comment Date: {comment.Date}");

                var commentAuthor = socialNetwork.GetUserById(comment.AuthorID);
                Console.WriteLine($"   Comment Author: {commentAuthor.FirstName} {commentAuthor.LastName}");
            }

            Console.WriteLine("\n");
        }
    }
}

