using System;
using System.Collections.Generic;

namespace MP3_ADU_namespace
{
    class MP3_ADU
    {
        //constructor initialize
        protected byte[] mp3_buff = null;
        protected int mp3_buff_length = 0;
        protected bool bValidMP3 = false;
        public bool BValidMP3 { get => bValidMP3; }
        public byte[] Mp3_buff { get => mp3_buff; }
        public int Mp3_buff_length { get => mp3_buff_length; }

        //mp3 is MPEG 1 Layer III or MPEG 2 layer III, detail: https://en.wikipedia.org/wiki/MP3

        //mp3 header include 32-bit
        // byte0    byte1   byte2   byte3
        //bit 31                        0
        //detail: http://www.mp3-tech.org/programmer/frame_header.html 

        //bit 31-21 is frame sync, all bit is 1, (byte0 == FF) && (byte1 & 0xE0 == 0xE0)

        //bit 20-19 : MPEG version, 11: V1, 10: V2
        int version, version_first_header;

        public int Version { get => version; }

        //bit 18-17 Layer, just consider layer III, 01
        const int layer = 3;
        public static int Layer => layer;

        bool HaveFirstHeader = false;

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

        int sideInfoSize;
        public int SideInfoSize { get => sideInfoSize; }

        //channel bit 7, 6; 00, 01, 10, 11 : stereo, joint stereo, dual channel, mono
        int channel;
        public int Channel { get => channel; }

        //v2 8-bit after header, v1 9-bit after header
        int main_data_begin;
        public int MainDataBegin { get => main_data_begin; }

        int totalFrame = 0;
        public int TotalFrame { get => totalFrame; }

        private bool IsValidHeader(byte[] buff, int i_buff, int buff_length)
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
            if (!HaveFirstHeader) //it is first frame
            {
                version_first_header = version;
                sample_rate_first_header = sample_rate;
                HaveFirstHeader = true;
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

            return true;
        }

        private void GetNextFrameSize(int i_buff)
        {
            //get infor header
            int header = (int)mp3_buff[i_buff + 3] | ((int)mp3_buff[i_buff + 2] << 8) | ((int)mp3_buff[i_buff + 1] << 16) | ((int)mp3_buff[i_buff] << 24);

            //get version
            int tmp = (header >> 19) & 0b11;
            if (tmp == 0b11)
                version = 1;
            else if (tmp == 0b10)
                version = 2;

            //get layer
            tmp = (header >> 17) & 0b11;

            //get bitrate
            tmp = (header >> 12) & 0b1111;

            if (version == 1)
                bitrate = bitrate_V1_L3[tmp];
            else if (version == 2)
                bitrate = bitrate_V2_L3[tmp];

            //get smaple rate
            tmp = (header >> 10) & 0b11;

            if (version == 1)
                sample_rate = sample_rate_V1[tmp];
            else if (version == 2)
                sample_rate = sample_rate_V2[tmp];

            //get padding
            padding = (header >> 9) & 1;

            //get sample per frame
            sample_per_frame = sample_per_frame_version[version - 1];

            //timePerFrame_ms
            timePerFrame_ms = 1000.0 * (double)sample_per_frame / (double)sample_rate;

            //get frame size
            double frame_size_tmp = bitrate * 1000 * sample_per_frame / 8 / sample_rate + padding;
            frame_size = (int)frame_size_tmp;

            //get channel
            channel = (header >> 6) & 3; //0b11

            //get side info size
            if (version == 1 && channel != 3) // v1, stereo
            {
                sideInfoSize = 32;
            }
            else if (version == 2 && channel == 3) //v2, mono
            {
                sideInfoSize = 9;
            }
            else
            {
                sideInfoSize = 17;
            }

            //get main data begin
            if (version == 1)
            {
                main_data_begin = ((int)mp3_buff[4 + i_buff] << 1) | (((int)mp3_buff[5 + i_buff] >> 7) & 1); // 9-bit
            }
            else
            {
                main_data_begin = (int)mp3_buff[4 + i_buff]; // 8-bit
            }
        }

        protected bool IsValidMp3()
        {
            if ((mp3_buff == null) || (mp3_buff_length < 1)) //invalid initialize
            {
                return false;
            }

            int index_buff_mp3 = 0, numOfFrame = 0;

            while (index_buff_mp3 < (mp3_buff_length - 3)) // a frame has at least 4 bytes
            {
                if ((mp3_buff[index_buff_mp3] == 0xFF) && ((mp3_buff[index_buff_mp3 + 1] & 0xE0) == 0xE0)) //sync bit
                {
                    if (IsValidHeader(mp3_buff, index_buff_mp3, mp3_buff_length))
                    {
                        index_buff_mp3 += frame_size;
                        numOfFrame++;
                        continue;
                    }
                }
                else
                {
                    if (HaveFirstHeader) return false;
                }
                index_buff_mp3++;
            }
            totalFrame = numOfFrame;
            return true;
        }

        bool bReadNextFrameFirst = true;
        //use this method, start_frame will point to next frame and update frame_size, bitrate, time per frame,... of new frame
        public bool ReadNextFrame()
        {
            if (!bValidMP3) //wrong mp3
            {
                return false;
            }

            if (bReadNextFrameFirst)
            {
                start_frame = 0;
                bReadNextFrameFirst = false;
            }
            else
            {
                start_frame += frame_size;
                if (start_frame >= mp3_buff_length) return false; //out of range
            }
            GetNextFrameSize(start_frame);
            return true;
        }

        //constructor
        public MP3_ADU(byte[] _Mp3_buff, int _Mp3_buff_length)
        {
            mp3_buff = _Mp3_buff;
            mp3_buff_length = _Mp3_buff_length;

            bValidMP3 = IsValidMp3();
        }
        public MP3_ADU()
        {
            mp3_buff = null;
            mp3_buff_length = 0;
        }
    }


    class ADU_frame : MP3_ADU
    {
        //byte[] adu_save = new byte[511];
        int adu_save_size = 0;
        const int adu_max = 511;
        List<byte[]> list_adu_save = new List<byte[]>();

        public byte[] ReadNextADUFrame()
        {
            if (ReadNextFrame())
            {


                byte[] ADU_frame_tmp = new byte[Frame_size + MainDataBegin];
                //copy frame header
                Buffer.BlockCopy(Mp3_buff, Start_frame, ADU_frame_tmp, 0, 4);
                //copy side infor
                Buffer.BlockCopy(Mp3_buff, Start_frame + 4, ADU_frame_tmp, 4, SideInfoSize);

                //clear main_data_begin = 0
                if (Version == 1) //9-bit
                {
                    //ADU_frame_tmp[4] = 0;
                    //ADU_frame_tmp[5] &= 0x7F; //0b 0111 1111
                }
                else //8-bit
                {
                    //ADU_frame_tmp[4] = 0;
                }
                //copy frame_data
                Buffer.BlockCopy(Mp3_buff, Start_frame + 4 + SideInfoSize, ADU_frame_tmp, 4 + SideInfoSize + MainDataBegin, Frame_size - 4 - SideInfoSize);

                //copy main_data_begin
                int index_adu_list = list_adu_save.Count - 1;
                int adu_offset = MainDataBegin;
                while (index_adu_list > -1)
                {
                    if (adu_offset > list_adu_save[index_adu_list].Length)
                    {
                        adu_offset -= list_adu_save[index_adu_list].Length;
                        Buffer.BlockCopy(list_adu_save[index_adu_list], 0, ADU_frame_tmp, 4 + SideInfoSize + adu_offset, list_adu_save[index_adu_list].Length);
                    }
                    else if (adu_offset > 0)
                    {
                        Buffer.BlockCopy(list_adu_save[index_adu_list], list_adu_save[index_adu_list].Length - adu_offset, ADU_frame_tmp, 4 + SideInfoSize, adu_offset);
                        break;
                    }
                    index_adu_list--;
                }
                int byte_array_size_tmp = Frame_size - 4 - SideInfoSize;
                byte[] byte_array_tmp = new byte[byte_array_size_tmp];
                Buffer.BlockCopy(Mp3_buff, Start_frame + 4 + SideInfoSize, byte_array_tmp, 0, byte_array_size_tmp);
                adu_save_size += byte_array_size_tmp;
                list_adu_save.Add(byte_array_tmp);
                if (list_adu_save.Count > 0 && (adu_save_size - list_adu_save[0].Length) > adu_max)
                {
                    list_adu_save.RemoveAt(0);
                }
                return ADU_frame_tmp;

            }
            else
            {
                return null;
            }
        }

        //put main_data_begin first
        public byte[] ReadNextADUFrame2()
        {
            if (ReadNextFrame())
            {
                int byte_array_size_tmp = Frame_size - 4 - SideInfoSize;
                byte[] byte_array_tmp = new byte[byte_array_size_tmp];
                Buffer.BlockCopy(Mp3_buff, Start_frame + 4 + SideInfoSize, byte_array_tmp, 0, byte_array_size_tmp);
                adu_save_size += byte_array_size_tmp;
                list_adu_save.Add(byte_array_tmp);
                if (list_adu_save.Count > 0 && (adu_save_size - list_adu_save[0].Length) > adu_max)
                {
                    list_adu_save.RemoveAt(0);
                }

                byte[] ADU_frame_tmp = new byte[Frame_size + MainDataBegin];
                //copy frame header
                Buffer.BlockCopy(Mp3_buff, Start_frame, ADU_frame_tmp, MainDataBegin, 4);
                //copy side infor
                Buffer.BlockCopy(Mp3_buff, Start_frame + 4, ADU_frame_tmp, 4 + MainDataBegin, SideInfoSize);

                //clear main_data_begin = 0
                if (Version == 1) //9-bit
                {
                    ADU_frame_tmp[4 + MainDataBegin] = 0;
                    ADU_frame_tmp[5 + MainDataBegin] &= 0x7F; //0b 0111 1111
                }
                else //8-bit
                {
                    ADU_frame_tmp[4 + MainDataBegin] = 0;
                }
                //copy frame_data
                Buffer.BlockCopy(Mp3_buff, Start_frame + 4 + SideInfoSize, ADU_frame_tmp, 4 + SideInfoSize + MainDataBegin, Frame_size - 4 - SideInfoSize);

                //copy main_data_begin
                int index_adu_list = list_adu_save.Count - 1;
                int adu_offset = MainDataBegin;
                while (index_adu_list > -1)
                {
                    if (adu_offset > list_adu_save[index_adu_list].Length)
                    {
                        adu_offset -= list_adu_save[index_adu_list].Length;
                        Buffer.BlockCopy(list_adu_save[index_adu_list], 0, ADU_frame_tmp, adu_offset, list_adu_save[index_adu_list].Length);
                    }
                    else if (adu_offset > 0)
                    {
                        Buffer.BlockCopy(list_adu_save[index_adu_list], list_adu_save[index_adu_list].Length - adu_offset, ADU_frame_tmp, 0, adu_offset);
                        break;
                    }
                    index_adu_list--;
                }
                return ADU_frame_tmp;

            }
            else
            {
                return null;
            }
        }


        //constructor
        public ADU_frame(byte[] _Mp3_buff, int _Mp3_buff_length)
        {
            mp3_buff = _Mp3_buff;
            mp3_buff_length = _Mp3_buff_length;

            bValidMP3 = IsValidMp3();
        }
        public ADU_frame()
        {
            mp3_buff = null;
            mp3_buff_length = 0;
        }
    }
}
