using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace truyenthanhServerWeb.Models
{
    public class Account
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }
        //[Required, MinLength(4), MaxLength(20)]
        public string Username { get; set; }
        //[Required, MinLength(8), MaxLength(20)]
        public string Password { get; set; }
    }
}
