using MongoDB.Driver;
using NetCoreServer;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Serialization;
using truyenthanhServerWeb.Models;
using truyenthanhServerWeb.Services;
using Xabe.FFmpeg.Downloader;

namespace truyenthanhServerWeb.ServerMp3
{
    public class UDPServer
    {
        private int portUDPBroadcast;
        private static int portFFmpeg;//portUserFFmpegStartFrom
        private static string pathSong;
        public static string PathSong { get => pathSong; }
        private static string _dataFile = @"appsettings.xml";
        private static string AdminPass;
        private static string[] typeFileSupport = new string[] { ".aac", ".mp3", ".m4a", ".wav", ".ogg", ".flac", ".wma" }; //aac, mp3, m4a, wav, ogg, flac, wma
        public static string[] TypeFileSupport { get => typeFileSupport; }

        public static List<User> _userList = new List<User>();
        public static HashSet<Devicemini> _deviceHashet = new HashSet<Devicemini>();

        private bool _bIsInitalizeDone = false;
        internal bool bIsInitalizeDone { get => _bIsInitalizeDone; }
        private int intervalCheckRequestUDP;

        public static int PortFFmpeg { get => portFFmpeg; }

        private UDPsocket udpSocket;

        private void updateFromDB()
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
                    DatabaseName = "TruyenThanh",
                    PathSong = "Song",
                    PassAdmin = "admin@2020",
                    PortUDPBroadcast = 2000,
                    PortFFmpeg = 11000,
                    IntervalCheckRequestUDP = 60 // ~ 60s
                };
                //using var stream = File.Create(_dataFile);
                //_serializer.Serialize(stream, _settingDB);
            }
            else
            {
                using var stream = File.OpenRead(_dataFile);
                _settingDB = _serializer.Deserialize(stream) as TruyenthanhDatabaseSettings;
            }
            AdminPass = _settingDB.PassAdmin;
            pathSong = _settingDB.PathSong;
            portUDPBroadcast = _settingDB.PortUDPBroadcast;
            portFFmpeg = _settingDB.PortFFmpeg;
            intervalCheckRequestUDP = _settingDB.IntervalCheckRequestUDP;
            var client = new MongoClient(_settingDB.ConnectionString);
            var database = client.GetDatabase(_settingDB.DatabaseName);

            IMongoCollection<Account> _accountDB = database.GetCollection<Account>(_settingDB.AccountCollectionName);
            List<Account>  accountList = new List<Account>(_accountDB.Find(account => true).ToList());

            //check if song root folder is existed?
            if (!Directory.Exists(pathSong))
            {
                Directory.CreateDirectory(pathSong); //create*/         
            }

            //initialize list user
            int tmpIndx = 0;
            foreach(Account ac in accountList)
            {
                _userList.Add(new User(tmpIndx, ac));

                //initialize list song
                UpdateSong(tmpIndx);

                tmpIndx++;
            }

            //initialize list device
            IMongoCollection<Device> _deviceDB = database.GetCollection<Device>(_settingDB.DeviceCollectionName);
            List<Device> deviceList = new List<Device>(_deviceDB.Find(dv => true).ToList());
            foreach(Device dv in deviceList)
            {
                //find ownerId of device
                _userList[dv.OwnerIndx].lDevice.Add(dv);

                //add to Hashet
                _deviceHashet.Add(new Devicemini(dv.Id, dv.OwnerIndx));
            }
        }

        public static bool CheckPassAdmin(string _pass)
        {
            if (_pass == AdminPass) return true;
            return false;
        }

        private static void UpdateSong(int tmpIndx)
        {
            //check if song user folder is existed?
            if (!Directory.Exists(_userList[tmpIndx].pathSong))
            {
                Directory.CreateDirectory(_userList[tmpIndx].pathSong); //create*/         
            }
            else //update song in folder
            {
                DirectoryInfo di = new DirectoryInfo(_userList[tmpIndx].pathSong);
                foreach (FileInfo file in di.GetFiles())
                {
                    //check file is supported type
                    if (typeFileSupport.Contains(file.Extension))
                    {
                        _userList[tmpIndx].lSong.Add(file.Name);
                    }
                    else
                    {
                        file.Delete();
                    }
                }
            }
        }

        public UDPServer() { }

        private void DownLoadFFmpeg()
        {
            //update ffmpeg bin
            try
            {
                FFmpegDownloader.GetLatestVersion(FFmpegVersion.Official);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        public void Run()
        {
            //update ffmpeg bin
            //DownLoadFFmpeg();
            updateFromDB();

            //launch UDP server
            udpSocket = new UDPsocket(IPAddress.Any, portUDPBroadcast, intervalCheckRequestUDP);

            //set UDPsocket for user
            foreach(var u in _userList)
            {
                u.SetUdpSocketForUser(udpSocket);
            }

            _bIsInitalizeDone = true;
        }
        //public static void SendAsync(EndPoint ep, byte[] buff, int offset, int length)
        //{
        //    udpSocket.SendAsync(ep, buff, offset, length);
        //}

        //public static void SendSync(EndPoint ep, byte[] buff, int offset, int length)
        //{
        //    udpSocket.Send(ep, buff, offset, length);
        //}

    }

    class UDPsocket : UdpServer
    {
        private int intervalCheckRequestUDP = 5; // second
        //static int sizeOfPacket;

        //2 thread
        Thread threadCheckRequest;

        //for threadListen
        //byte[] receive_buffer = new byte[100];
        // biến này về sau sẽ chứa địa chỉ của tiến trình client nào gửi gói tin tới
        //EndPoint receive_IPEndPoint = new IPEndPoint(IPAddress.Any, 0);

        public UDPsocket(IPAddress address, int port, int _intervalCheckRequestUDP) : base(address, port)
        {
            Start();
            UDPsocketCheckRequest(_intervalCheckRequestUDP);
        }

        protected override void OnStarted()
        {
            // Start receive datagrams
            ReceiveAsync();
        }
        protected override void OnReceived(EndPoint endpoint, byte[] buffer, long offset, long size)
        {
            //Console.WriteLine("Incoming: " + Encoding.UTF8.GetString(buffer, (int)offset, (int)size));

            if (size >= 24)
            {
                //get id client
                var ID_client_received = Encoding.ASCII.GetString(buffer, (int)offset, 24);

                //reserved handle Data encrypted

                //check client in Hashet
                var dvDetected = UDPServer._deviceHashet.FirstOrDefault(dv => dv.Id == ID_client_received);
                if (dvDetected != null)
                {
                    if (dvDetected.OwnerIndx > -1 && dvDetected.OwnerIndx < UDPServer._userList.Count)
                    {
                        var dvIndxSearched = UDPServer._userList[dvDetected.OwnerIndx].lDevice.FindLastIndex(dv => dv.Id == dvDetected.Id);
                        if (dvIndxSearched != -1)
                        {
                            UDPServer._userList[dvDetected.OwnerIndx].lDevice[dvIndxSearched].deviceEndpoint.IPEndPoint_client = endpoint;
                            UDPServer._userList[dvDetected.OwnerIndx].lDevice[dvIndxSearched].deviceEndpoint.TimeStamp = DateTime.Now;
                            UDPServer._userList[dvDetected.OwnerIndx].lDevice[dvIndxSearched].deviceEndpoint.TimeOut = false;
                        }
                    }
                }
            }

            // Echo the message back to the sender
            //SendAsync(endpoint, buffer, 0, size);
            ReceiveAsync();
        }

        //public void SendArrayAsync(EndPoint endpoint, byte[] data, int offset, int length)
        //{
        //    if (offset + length > data.Length) return;
        //    SendAsync(endpoint, data, offset, length);
        //}

        //protected override void OnSent(EndPoint endpoint, long sent)
        //{
        //    // Continue receive datagrams
        //    ReceiveAsync();
        //}

        protected override void OnError(SocketError error)
        {
            Console.WriteLine($"Main UDP server caught an error with code {error}");
        }
        private void UDPsocketCheckRequest(int _intervalCheckRequestUDP)
        {
            intervalCheckRequestUDP = _intervalCheckRequestUDP;
            threadCheckRequest = new Thread(() =>
            {
                threadCheckRequestFunc();
            });

            threadCheckRequest.Priority = ThreadPriority.Lowest;
            threadCheckRequest.Start();
        }

        private void threadCheckRequestFunc()
        {
            DateTime nowMark;
            int offsetTime;
            
            while (true)
            {
                nowMark = DateTime.Now;
                foreach (User u in UDPServer._userList)
                {
                    if (u != null && u.lDevice != null)
                    {
                        foreach (Device dv in u.lDevice)
                        {
                            offsetTime = (int)(nowMark - dv.deviceEndpoint.TimeStamp).TotalSeconds;
                            if (offsetTime > intervalCheckRequestUDP)
                                dv.deviceEndpoint.TimeOut = true;
                        }
                    }
                }

                Thread.Sleep(intervalCheckRequestUDP * 1000); //check every intervalCheckReuestUDP seconds
            }
        }

        //private void threadSendFunc()
        //{
        //    status = status_enum.PLAY;
        //    while (true)
        //    {
        //        for (int i = 0; i < soundList.Count; i++)
        //        {
        //            HeaderPacket.IDsong = (byte)(i + 1);
        //            HeaderPacket.IDframe = 0;
        //            int value_control = 0;
        //            byte[] mp3_data;
        //            try
        //            {
        //                mp3_data = File.ReadAllBytes(soundList[i].FilePath);

        //                byte[] mp3_buff = File.ReadAllBytes(soundList[i].FilePath).Skip(237).ToArray();
        //                //MP3_ADU mp3file = new MP3_ADU(mp3_buff, mp3_buff.Length);
        //                //int numFrame = 0;

        //                ADU_frame adufile = new ADU_frame(mp3_buff, mp3_buff.Length);
        //                int aduNumFrame = 0;

        //                //FileStream stream = new FileStream(@"E:\test10.mp3", FileMode.Append);

        //                List<byte[]> _aduFrameList = new List<byte[]>();
        //                while (true)
        //                {
        //                    byte[] aduframe = adufile.ReadNextADUFrame();
        //                    if (aduframe != null)
        //                    {
        //                        aduNumFrame++;
        //                        _aduFrameList.Add(aduframe);
        //                    }
        //                    else
        //                    {
        //                        break;
        //                    }
        //                }

        //                aduFrameList = _aduFrameList;

        //            }

        //            catch (Exception ex)
        //            {
        //                Console.WriteLine(ex);
        //                continue;
        //            }
        //            songID = i;
        //            value_control = sendPacketMP3(mp3_data, mp3_data.Length);

        //            if (value_control == 3) //next
        //            {
        //                Thread.Sleep(1000);
        //            }
        //            else if (value_control == 4) //previuos
        //            {
        //                if (i > 0)
        //                {
        //                    i -= 2;
        //                    //continue;
        //                }
        //                else if (i == 0)
        //                {
        //                    i = soundList.Count - 2;
        //                }
        //            }
        //            else if (value_control == 5) //stop
        //            {
        //                songID = 0;
        //                timePlaying_song_s = 0;
        //                duration_song_s = 0;
        //                return;
        //            }

        //        }
        //        if (!loopBack)
        //        {
        //            break;
        //        }
        //    }
        //}

        //private int sendPacketMP3(byte[] mp3_buff, int mp3_buff_length)
        //{

        //    double timePoint, mark_time = 0;

        //    MP3_frame mp3_reader = new MP3_frame(mp3_buff, mp3_buff_length);
        //    if (!mp3_reader.IsValidMp3())
        //    {
        //        return -1;
        //    }
        //    //const double framemp3_time = (double)1152.0 * 1000.0 / 44100.0; //ms
        //    double framemp3_time = mp3_reader.TimePerFrame_ms;
        //    //count total Frame of mp3
        //    duration_song_s = mp3_reader.countFrame() * (int)mp3_reader.TimePerFrame_ms / 1000;

        //    int orderFrame = 0;

        //    SocketFlags socketFlag = new SocketFlags();

        //    //launch timer
        //    Stopwatch stopWatchSend = new Stopwatch();
        //    stopWatchSend.Start();

        //    int tmp;
        //    while (true)
        //    {

        //        byte[] sendADU = packet_udp_frameADU(orderFrame);
        //        bool endOfFile = false;
        //        //byte[] sendADU = new byte[200];
        //        //sendADU[0] = (byte)'h';
        //        //sendADU[1] = (byte)'i';

        //        //change status, return value to threadSendFunc
        //        //value: 1-play-next, 2-play-previous, 3-stop
        //        tmp = resumeNextPrevious;
        //        if (status == status_enum.STOP)
        //        {
        //            tmp = 5;
        //            sendADU = null;
        //        }
        //        else if (resumeNextPrevious > 0)
        //        {
        //            resumeNextPrevious = 0;
        //            if (tmp == 2)
        //            {
        //                stopWatchSend.Start();
        //            }
        //            else if (tmp > 4)
        //            {
        //                tmp = 5;
        //            }
        //            sendADU = null;
        //        }
        //        else if (status == status_enum.PAUSE)
        //        {
        //            stopWatchSend.Stop();
        //            Thread.Sleep(500);
        //            sendADU = null;
        //        }

        //        if (sendADU == null)
        //        {
        //            //sendADU = new byte[10];
        //            //sendADU[0] = 0xAA;
        //            endOfFile = true;
        //            break;
        //        }
        //        for (int i = 0; i < clientList.Count; i++)
        //        {
        //            if ((!clientList[i].TimeOut) && (clientList[i].On))
        //            {
        //                for (int j = 0; j < clientList[i].NumSend; j++)
        //                {
        //                    try
        //                    {
        //                        socket.SendAsync(clientList[i].IPEndPoint_client, sendADU, 0, sendADU.Length);
        //                        //Console.WriteLine("$");
        //                    }
        //                    catch (Exception ex)
        //                    {
        //                        Console.WriteLine(ex);
        //                    }
        //                }
        //            }
        //        }

        //        //send 2 times ~ retrans
        //        for (int i = 0; i < listTestClientEndPoint.Count; i++)
        //        {
        //            try
        //            {
        //                socket.SendAsync(listTestClientEndPoint[i], sendADU);
        //                //listTestClientSocket[i].BeginSend(sendADU, 0, sendADU.Length, SocketFlags.None, SendCallback, listTestClientSocket[i]);
        //            }
        //            catch (Exception ex)
        //            {
        //                Console.WriteLine(ex);
        //            }
        //        }

        //        if (endOfFile) break;

        //        orderFrame++;

        //        //Console.WriteLine(orderFrame);
        //        mark_time = 24 * orderFrame; //point to next time frame

        //        //get current time playing
        //        timePlaying_song_s = (int)mark_time / 1000; //second
        //        timePoint = mark_time - stopWatchSend.Elapsed.TotalMilliseconds;
        //        //if (timePoint < 0) Console.WriteLine(timePoint);
        //        if (timePoint > 0)
        //        {
        //            Thread.Sleep((int)timePoint);
        //        }
        //    }

        //    //done a song
        //    timePlaying_song_s = 0;
        //    duration_song_s = 0;
        //    stopWatchSend.Stop();
        //    Thread.Sleep(500);
        //    return tmp;
        //}

        //static private int packet_udp_frameMP3(byte[] _send_buff, MP3_frame _mp3_reader)
        //{
        //    //reset packet header
        //    HeaderPacket.NumOffFrame = 0;
        //    HeaderPacket.TotalLength = HeaderPacket.Length;
        //    while (true)
        //    {
        //        if (left_frame_not_packet)
        //        {
        //            left_frame_not_packet = false;
        //        }
        //        else if (_mp3_reader.ReadNextFrame())
        //        {

        //        }
        //        else
        //        {
        //            break;
        //        }
        //        //check space for cmemcpy //(4+4) for numOfFrame, totalLength
        //        if (_mp3_reader.Frame_size <= (Max_send_buff_length - HeaderPacket.TotalLength))
        //        {
        //            System.Buffer.BlockCopy(_mp3_reader.Mp3_buff, _mp3_reader.Start_frame, _send_buff, HeaderPacket.TotalLength, _mp3_reader.Frame_size);
        //            HeaderPacket.TotalLength += (UInt16)(_mp3_reader.Frame_size);
        //            HeaderPacket.NumOffFrame++;
        //        }
        //        else
        //        {
        //            left_frame_not_packet = true;
        //            break;
        //        }
        //    }

        //    //copy header to _send_buff
        //    HeaderPacket.copyHeaderToBuffer(_send_buff);

        //    sizeOfPacket = HeaderPacket.TotalLength;

        //    //increase IDframe
        //    HeaderPacket.IDframe++;

        //    return HeaderPacket.NumOffFrame;
        //}
        ////static int[] orderArray = { 0, 2, 4, 6, 1, 3, 5, 7 };
        //static int[] orderArray = { 0, 1, 2, 3, 4, 5, 6, 7 }; // no interleave
        //static int orderArrayIndex = 0;
        //static int packetIndex = 0;
        //static private byte[] packet_udp_frameADU(int _numOfFrame)
        //{
        //    if (_numOfFrame == aduFrameList.Count)
        //    {
        //        return null;
        //    }

        //    //int iListADU = (_numOfFrame / 8) * 8 + orderArray[orderArrayIndex];
        //    int iListADU = _numOfFrame;
        //    int sizeOfADUpacket = aduFrameList[iListADU].Length + 2 + 4 + 4; //2B checksum, 4B id adu frame, 4B idSong
        //    byte[] tmpADUpacket = new byte[sizeOfADUpacket];
        //    //copy adu id
        //    System.Buffer.BlockCopy(BitConverter.GetBytes(iListADU + 1), 0, tmpADUpacket, 2, 4); //start from 1
        //    //copy song id
        //    System.Buffer.BlockCopy(BitConverter.GetBytes(songID + 1), 0, tmpADUpacket, 2 + 4, 4);
        //    //copy adu data
        //    System.Buffer.BlockCopy(aduFrameList[iListADU], 0, tmpADUpacket, 2 + 4 + 4, aduFrameList[iListADU].Length);
        //    //checksum
        //    UInt16 checkSum = caculateChecksum(tmpADUpacket, 2, 4 + 4 + aduFrameList[iListADU].Length);
        //    //copy checksum
        //    System.Buffer.BlockCopy(BitConverter.GetBytes(checkSum), 0, tmpADUpacket, 0, 2);

        //    return tmpADUpacket;
        //}

        public UInt16 caculateChecksum(byte[] data, int offset, int length)
        {
            UInt32 checkSum = 0;
            int index = offset;
            while (length > 1)
            {
                checkSum += ((UInt32)data[index] << 8) | ((UInt32)data[index + 1]); //little edian
                length -= 2;
                index += 2;
            }
            if (length == 1) // still have 1 byte
            {
                checkSum += ((UInt32)data[index] << 8);
            }
            while ((checkSum >> 16) > 0) //checkSum > 0xFFFF
            {
                checkSum = (checkSum & 0xFFFF) + (checkSum >> 16);
            }
            //inverse
            checkSum = ~checkSum;
            return (UInt16)checkSum;
        }
    }

    class headerPacket
    {
        ////header of UDP packet
        //1-byte: volume, 1-byte: ID_song, 2-byte: totalLength
        //4-byte: ID_frame
        //2-byte: numOfFrame, 2-byte checksum
        //de cho an toan, nen tinh checksum cho header nay

        //total byte in header
        UInt16 length = 14;
        byte volume = 0x00; // max:min 0x00:0xFE
        byte id_song;
        UInt16 totalLength;
        UInt32 id_frame;
        UInt16 numOffFrame;
        UInt16 checkSum;
        UInt16 checkSumData;

        public UInt16 Length { get => length; }

        internal byte IDsong { get => id_song; set => id_song = value; }
        internal ushort TotalLength { get => totalLength; set => totalLength = value; }
        internal uint IDframe { get => id_frame; set => id_frame = value; }
        internal ushort NumOffFrame { get => numOffFrame; set => numOffFrame = value; }

        internal byte Volume
        {
            get { return volume; }
            set
            {
                if (value == 0xFF)
                    volume = 0xFE;
                else
                    volume = value;
            }
        }

        internal void copyHeaderToBuffer(byte[] _buffer)
        {
            _buffer[0] = volume;
            _buffer[1] = id_song;
            byte[] tmp_byte = new byte[4];
            tmp_byte = BitConverter.GetBytes(totalLength);
            System.Buffer.BlockCopy(tmp_byte, 0, _buffer, 2, 2);
            tmp_byte = BitConverter.GetBytes(id_frame);
            System.Buffer.BlockCopy(tmp_byte, 0, _buffer, 4, 4);
            tmp_byte = BitConverter.GetBytes(numOffFrame);
            System.Buffer.BlockCopy(tmp_byte, 0, _buffer, 8, 2);

            //caculate checksum for header and checksum for data
            checkSum = caculateChecksum(_buffer, 0, length - 4); //header
            checkSumData = caculateChecksum(_buffer, length, totalLength - length);

            tmp_byte = BitConverter.GetBytes(checkSum);
            System.Buffer.BlockCopy(tmp_byte, 0, _buffer, 10, 2);
            tmp_byte = BitConverter.GetBytes(checkSumData);
            System.Buffer.BlockCopy(tmp_byte, 0, _buffer, 12, 2);
        }

        static UInt16 caculateChecksum(byte[] data, int offset, int length)
        {
            UInt32 checkSum = 0;
            int index = offset;
            while (length > 1)
            {
                checkSum += ((UInt32)data[index] << 8) | ((UInt32)data[index + 1]); //little edian
                length -= 2;
                index += 2;
            }
            if (length == 1) // still have 1 byte
            {
                checkSum += ((UInt32)data[index] << 8);
            }
            while ((checkSum >> 16) > 0) //checkSum > 0xFFFF
            {
                checkSum = (checkSum & 0xFFFF) + (checkSum >> 16);
            }
            //inverse
            checkSum = ~checkSum;
            return (UInt16)checkSum;
        }

    }
}
