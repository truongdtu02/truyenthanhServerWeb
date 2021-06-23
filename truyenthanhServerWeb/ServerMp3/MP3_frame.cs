namespace UDP_send_packet_frame
{
    class MP3_frame
    {
        //constructor initialize
        byte[] mp3_buff = null;
        int mp3_buff_length = 0;
        bool IsValidMP3_bool = false;
        public byte[] Mp3_buff { get => mp3_buff; }
        public int Mp3_buff_length { get => mp3_buff_length; }

        //mp3 is MPEG 1 Layer III or MPEG 2 layer III, detail: https://en.wikipedia.org/wiki/MP3

        //mp3 header include 32-bit
        // byte0    byte1   byte2   byte3
        //bit 31                        0
        //detail: http://www.mp3-tech.org/programmer/frame_header.

        //bit 31-21 is frame sync, all bit is 1, (byte0 == FF) && (byte1 & 0xE0 == 0xE0)

        //bit 20-19 : MPEG version, 11: V1, 10: V2
        int version, version_first_header;

        public int Version { get => version; }

        //bit 18-17 Layer, just consider layer III, 01
        const int layer = 3;
        public static int Layer => layer;



        //bit 16, protected bit, don't count

        //bit 15-12 bitrate
        static readonly int[] bitrate_V1_L3 = { 0, 32, 40, 48, 56, 64, 80, 96, 112, 128, 160, 192, 224, 256, 320, 0 }; // MPEG 1, layer III
        static readonly int[] bitrate_V2_L3 = { 0, 8, 16, 24, 32, 40, 48, 56, 64, 80, 96, 112, 128, 144, 160, 0 }; // MPEG 2, layer III
        int bitrate;
        public int Bitrate { get => bitrate; }


        //bit 11-10 sample rate
        static readonly int[] sample_rate_V1 = { 44100, 48000, 32000, 0 };
        static readonly int[] sample_rate_V2 = { 22050, 24000, 16000, 0 };
        int sample_rate, sample_rate_first_header;
        public int Sample_rate { get => sample_rate; }

        //sample per frame
        int sample_per_frame;
        static readonly int[] sample_per_frame_version = { 1152, 576 };

        public int Sample_per_frame { get => sample_per_frame; }

        //bit 9, padding bit
        int padding;
        public int Padding { get => padding; }

        int frame_size = 0;
        public int Frame_size { get => frame_size; }

        int start_frame = 0, end_frame = 0;
        double timePerFrame_ms;
        public double TimePerFrame_ms { get => timePerFrame_ms; }
        public int Start_frame { get => start_frame; }
        public int End_frame { get => end_frame; }


        public bool IsValidHeader(byte[] buff, int i_buff, int buff_length)
        {
            //get infor header
            int header = (int)buff[i_buff + 3] | ((int)buff[i_buff + 2] << 8) | ((int)buff[i_buff + 1] << 16) | ((int)buff[i_buff] << 24);

            //get version
            int tmp = (header >> 19) & 0b11;
            if (tmp == 0b11)
                version = 1;
            else if (tmp == 0b10)
                version = 2;
            else
                return false;

            //get layer
            tmp = (header >> 17) & 0b11;
            if (tmp != 0b01) //layer III
                return false;

            //get bitrate
            tmp = (header >> 12) & 0b1111;
            if ((tmp == 0) || (tmp == 0b1111))
                return false;
            if (version == 1)
                bitrate = bitrate_V1_L3[tmp];
            else if (version == 2)
                bitrate = bitrate_V2_L3[tmp];

            //get smaple rate
            tmp = (header >> 10) & 0b11;
            if (tmp == 0b11)
                return false;
            if (version == 1)
                sample_rate = sample_rate_V1[tmp];
            else if (version == 2)
                sample_rate = sample_rate_V2[tmp];

            //check, if it is next frame, compare with first frame
            if (!IsValidMP3_bool) //it is first frame
            {
                version_first_header = version;
                sample_rate_first_header = sample_rate;
            }
            else //next frame
            {
                if ((version_first_header != version) || (sample_rate_first_header != sample_rate))
                    return false;
            }

            //get padding
            padding = (header >> 9) & 1;

            //get sample per frame
            sample_per_frame = sample_per_frame_version[version - 1];

            //timePerFrame_ms
            timePerFrame_ms = 1000.0 * (double)sample_per_frame / (double)sample_rate;

            //get frame size
            double frame_size_tmp = bitrate * 1000 * sample_per_frame / 8 / sample_rate + padding;
            frame_size = (int)frame_size_tmp;

            //check next frame
            if ((i_buff + frame_size) > buff_length) //out of range
            {
                return false;
            }
            start_frame = i_buff;
            return true;
        }

        public int countFrame()
        {
            if (!IsValidMP3_bool) //wrong mp3
            {
                return 0;
            }

            int index_buff_mp3 = 0, totalFrame = 0;

            while (index_buff_mp3 < (mp3_buff_length - 3)) // a frame has at least 4 bytes
            {
                if ((mp3_buff[index_buff_mp3] == 0xFF) && ((mp3_buff[index_buff_mp3 + 1] & 0xE0) == 0xE0)) //sync bit
                {
                    if (IsValidHeader(mp3_buff, index_buff_mp3, mp3_buff_length))
                    {
                        index_buff_mp3 = start_frame + frame_size;
                        totalFrame++;
                        continue;
                    }
                }
                index_buff_mp3++;
            }
            //reset frame_size and start frame for read next frame
            frame_size = 0;
            start_frame = 0;
            return totalFrame;
        }

        public bool IsValidMp3()
        {
            if ((mp3_buff == null) || (mp3_buff_length < 1)) //invalid initialize
            {
                return false;
            }
            int index_buff_mp3 = 0;
            while (index_buff_mp3 < (mp3_buff_length - 3)) // a frame has at least 4 bytes
            {
                if ((mp3_buff[index_buff_mp3] == 0xFF) && ((mp3_buff[index_buff_mp3 + 1] & 0xE0) == 0xE0)) //sync bit
                {
                    if (IsValidHeader(mp3_buff, index_buff_mp3, mp3_buff_length))
                    {
                        IsValidMP3_bool = true;
                        //reset frame_size, because in IsValidHeader, it change frame_size, but we just want check,
                        // not get header infor
                        frame_size = 0;
                        start_frame = 0;
                        return true;
                    }
                }
                index_buff_mp3++;
            }
            return false;
        }

        public bool ReadNextFrame()
        {
            if (!IsValidMP3_bool) //wrong mp3
            {
                return false;
            }

            int index_buff_mp3 = start_frame + frame_size;

            while (index_buff_mp3 < (mp3_buff_length - 3)) // a frame has at least 4 bytes
            {
                if ((mp3_buff[index_buff_mp3] == 0xFF) && ((mp3_buff[index_buff_mp3 + 1] & 0xE0) == 0xE0)) //sync bit
                {
                    if (IsValidHeader(mp3_buff, index_buff_mp3, mp3_buff_length))
                    {
                        return true;
                    }
                }
                index_buff_mp3++;
            }
            return false;
        }

        //constructor
        public MP3_frame(byte[] _Mp3_buff, int _Mp3_buff_length)
        {
            mp3_buff = _Mp3_buff;
            mp3_buff_length = _Mp3_buff_length;
        }
        public MP3_frame()
        {
            mp3_buff = null;
            mp3_buff_length = 0;
        }
    }

}
