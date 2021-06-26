using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace truyenthanhServerWeb.Models
{
    public class Device
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }
        //[Required, MinLength(4), MaxLength(20)]
        public string Name { get; set; }
        public int OwnerIndx { get; set; } //index in _userList, don't allow edit in UI
        [BsonIgnore]
        public DeviceEndpoint deviceEndpoint = new DeviceEndpoint();

        //public Device(string id, string name, int ownerIndx)
        //{
        //    Id = id;
        //    Name = name;
        //    OwnerIndx = ownerIndx;
        //}
    }

    public class DeviceEndpoint
    {
        // biến này về sau sẽ chứa địa chỉ của tiến trình client nào gửi gói tin tới
        EndPoint ipEndPoint_client = new IPEndPoint(IPAddress.Any, 0);
        public EndPoint IPEndPoint_client { get => ipEndPoint_client; set => ipEndPoint_client = value; }

        DateTime timeStamp = DateTime.Now;

        bool timeOut = true; //timeOut = true, that mean don't receive request in last 5s, and don't send

        bool on = true; //change this on app

        //server just sends to client when timeOut == false and On == true

        public DateTime TimeStamp { get => timeStamp; set => timeStamp = value; }
        public bool TimeOut { get => timeOut; set => timeOut = value; }
        public bool On { get => on; set => on = value; }
    }


    public class Devicemini
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }
        public int OwnerIndx { get; set; } //index in _userList, don't allow edit in UI
        public Devicemini(string id, int indx)
        {
            Id = id;
            OwnerIndx = indx;
        }

    }
}
