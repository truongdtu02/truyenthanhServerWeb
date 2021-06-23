﻿using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Serialization;
using truyenthanhServerWeb.Models;
using truyenthanhServerWeb.Services;
using UDP_send_packet_frame;

namespace truyenthanhServerWeb.ServerMp3
{
    public class UDPServer
    {
        private static string _dataFile = @"UDPserver\dsetting.xml";

        public static List<User> _userList = new List<User>();

        private static void updateFromDB()
        {
            TruyenthanhDatabaseSettings _settingDB;
            XmlSerializer _serializer = new XmlSerializer(typeof(TruyenthanhDatabaseSettings));
            if (!File.Exists(_dataFile))
            {
                _settingDB = new TruyenthanhDatabaseSettings()
                {
                    AccountCollectionName = "Account",
                    DeviceCollectionName = "Device",
                    ConnectionString = "mongodb://localhost:27017",
                    DatabaseName = "TruyenThanh"
                };
            }
            else
            {
                using var stream = File.OpenRead(_dataFile);
                _settingDB = _serializer.Deserialize(stream) as TruyenthanhDatabaseSettings;
            }

            var client = new MongoClient(_settingDB.ConnectionString);
            var database = client.GetDatabase(_settingDB.DatabaseName);

            IMongoCollection<Account> _accountDB = database.GetCollection<Account>(_settingDB.AccountCollectionName);
            List<Account>  accountList = new List<Account>(_accountDB.Find(account => true).ToList());

            //initialize list user
            foreach(Account ac in accountList)
            {
                _userList.Add(new User(ac));
            }
        }
        public UDPServer()
        {
            updateFromDB();
            //Account ac = _accountList[0];

            // Subscription to account database record change events
            //AccountService.AccountChanged += UpdateAccountList;

            List<client_IPEndPoint>  clientList = new List<client_IPEndPoint>()
            {
                 new client_IPEndPoint(){ ID_client = "20154023", On = true, NumSend = 1},
                 new client_IPEndPoint(){ ID_client = "20164023", On = false},
                 new client_IPEndPoint(){ ID_client = "sim", On = true, NumSend = 1},
            };

            //launch
            //UDPsocket udpSocket = new UDPsocket();
            //List<soundTrack> soundList = new List<soundTrack>();
            //udpSocket.launchUDPsocket(soundList, clientList);
        }

        public void Run()
        {
            Thread.Sleep(30000);
            Thread.Sleep(10000);
            Thread.Sleep(10000);
        }

        //public void UpdateAccountList(object sender, AccountChangedEventArgs args)
        //{

        //}

        public static void UpdateAccountList()
        {

        }

    }
}