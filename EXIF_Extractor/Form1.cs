using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Text;
using System.Windows.Forms;

namespace EXIF_Extractor
{
    public partial class Form1 : Form
    {
        System.Threading.Thread t1;

        DataTable DT;
        DataTable DT_output;

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            genPropertyList();
            this.status.Text = "";
            Application.ApplicationExit += new EventHandler(Application_ApplicationExit);
        }

        void Application_ApplicationExit(object sender, EventArgs e)
        {
            if (t1 != null)
                t1.Abort();
        }

        //원본경로 선택
        private void button1_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog folderBrowser = new FolderBrowserDialog();
            if (folderBrowser.ShowDialog() == DialogResult.OK)
                sourcePath.Text = folderBrowser.SelectedPath;
        }

        //EXIF 추출
        private void button2_Click(object sender, EventArgs e)
        {
            t1 = new System.Threading.Thread(new System.Threading.ThreadStart(RunExtract));
            t1.Start();
        }

        private delegate void Form2CallBack();

        private void RunExtract()
        {
            libMyUtil.clsThread.buttonToggle(this.button2, "실행중", false);

            genDToutput();
            getFileList(sourcePath.Text, SearchOption.AllDirectories, "*.jpg;*.jpeg;*.png");
            /*
            Form2 objForm2 = new Form2(DT_output);
            objForm2.ShowDialog();
             */
            MessageBox.Show("완료");

            libMyUtil.clsThread.buttonToggle(this.button2, "실행", true);
        }

        private void genDToutput()
        {
            DT_output = new DataTable();
            DT_output.Columns.Add("fileName");
            
            for (int i = 0; i < DT.Rows.Count; i++)
                DT_output.Columns.Add(DT.Rows[i][1].ToString());
        }

        /// <summary>
        /// 해당path에서 일정 패턴의 파일 목록 반환
        /// </summary>
        /// <param name="path"></param>
        /// <param name="srchOption">서브디렉토리 포함 여부</param>
        /// <param name="srchPattern">";"로 구분된 복수의 패턴</param>
        /// <returns></returns>
        private void getFileList(string path, SearchOption objSrchoption, string srchPattern)
        {
            libCommon.clsUtil objUtil = new libCommon.clsUtil();
            System.Collections.ArrayList arrList = new ArrayList();
            string[] fileExts = objUtil.Split(srchPattern, ";");

            for (int i = 0; i < fileExts.Length; i++)
            {
                string[] files = Directory.GetFiles(path, fileExts[i], objSrchoption);
                string statusStr1 = string.Format("STEP:{0}/{1}...", i + 1, fileExts.Length);
                string statusStr2 = "0/0";
                
                if (files.Length > 0)
                {
                    for (int j = 0; j < files.Length; j++)
                    {
                        getEXIFinfo(files[j]);
                        if (j % 10 == 0 || j == files.Length - 1)
                        {
                            statusStr2 = string.Format("{0}/{1}", j + 1, files.Length);
                            libMyUtil.clsThread.SetLabel(this.status, statusStr1 + statusStr2);
                        }
                    }
                }

                libMyUtil.clsThread.SetTextBox(this.textBox1, statusStr1 + statusStr2 + "\r\n");
            }
        }

        private void getEXIFinfo(string fileName)
        {
            string fileExtension;
            StringBuilder strBuilder = new StringBuilder();

            fileExtension = Path.GetExtension(fileName).ToLower(); ;

            if (fileExtension.Equals(".jpg") || fileExtension.Equals(".jpeg") || fileExtension.Equals(".png"))
            {
                System.Drawing.Image theImage = null;
                System.Drawing.Imaging.PropertyItem[] propItems = null;

                try
                {
                    theImage = new System.Drawing.Bitmap(fileName);
                    propItems = theImage.PropertyItems;
                }
                catch (Exception ex)
                {
                    libMyUtil.clsFile.writeLog("ERR OPEN IMAGE : " + fileName + "\r\n" + ex.ToString());
                    return;
                }

                /*
                DT_output.Rows.Add();
                DT_output.Rows[DT_output.Rows.Count - 1]["fileName"] = fileName;
                */

                if (propItems != null && propItems.Length > 0)
                {
                    string width;
                    string height;
                    width = theImage.Size.Width.ToString();
                    height = theImage.Size.Height.ToString();

                    string date = "";
                    string model = "";

                    for (int i = 0; i < propItems.Length; i++)
                    {
                        string propName;
                        string propValue;
                        short propType;

                        

                        propName = getPropertyName("0x" + Convert.ToString(propItems[i].Id, 16).PadLeft(4, '0').ToUpper());

                        if (propName.Length > 0)
                        {
                            propType = propItems[i].Type;

                            switch (propType)
                            {
                                case 1:
                                    propValue = propItems[i].Value.GetValue(0).ToString();
                                    break;
                                case 2:
                                    propValue = System.Text.Encoding.ASCII.GetString(propItems[i].Value).Replace("\0", "");
                                    break;
                                case 3:
                                    if (propItems[i].Value.Length < 2)
                                        propValue = propItems[i].Value.GetValue(0).ToString();
                                    else
                                        propValue = BitConverter.ToUInt16(propItems[i].Value, 0).ToString();
                                    break;
                                case 4:
                                    if (propItems[i].Value.Length < 4)
                                        propValue = propItems[i].Value.GetValue(0).ToString();
                                    else
                                        propValue = BitConverter.ToUInt32(propItems[i].Value, 0).ToString();
                                    break;
                                case 5:
                                    if (propItems[i].Value.Length < 8)
                                        propValue = propItems[i].Value.GetValue(0).ToString();
                                    else if (propItems[i].Value.Length == 8)
                                    {
                                        propValue = BitConverter.ToUInt32(propItems[i].Value, 0).ToString();
                                        propValue += "/" + BitConverter.ToUInt32(propItems[i].Value, 4).ToString();
                                    }
                                    else
                                    {
                                        propValue = BitConverter.ToUInt32(propItems[i].Value, 0).ToString();
                                        propValue += "/" + BitConverter.ToUInt32(propItems[i].Value, 4).ToString();
                                        propValue += ", " + BitConverter.ToUInt32(propItems[i].Value, 8).ToString();
                                        propValue += "/" + BitConverter.ToUInt32(propItems[i].Value, 12).ToString();
                                    }
                                    break;
                                case 6:
                                    propValue = propItems[i].Value.Length + "Bytes";
                                    break;
                                case 7:
                                    if (propItems[i].Value.Length < 4)
                                        propValue = propItems[i].Value.GetValue(0).ToString();
                                    else
                                        propValue = BitConverter.ToInt32(propItems[i].Value, 0).ToString();
                                    break;
                                case 10:
                                    if (propItems[i].Value.Length < 8)
                                        propValue = propItems[i].Value.GetValue(0).ToString();
                                    else if (propItems[i].Value.Length == 8)
                                    {
                                        propValue = BitConverter.ToUInt32(propItems[i].Value, 0).ToString();
                                        propValue += "/" + BitConverter.ToUInt32(propItems[i].Value, 4).ToString();
                                    }
                                    else
                                    {
                                        propValue = BitConverter.ToUInt32(propItems[i].Value, 0).ToString();
                                        propValue += "/" + BitConverter.ToUInt32(propItems[i].Value, 4).ToString();
                                        propValue += ", " + BitConverter.ToUInt32(propItems[i].Value, 8).ToString();
                                        propValue += "/" + BitConverter.ToUInt32(propItems[i].Value, 12).ToString();
                                    }
                                    break;
                                default:
                                    propValue = "Unknown";
                                    break;
                            }

                            if (propValue.Trim().Length > 0)
                            {
                                /*
                                if (strBuilder.Length > 0)
                                    strBuilder.Append("; ");
                                strBuilder.Append(propName + ":" + propValue);
                                 */
                                if (propName.Equals("EquipModel"))
                                    model = propValue;
                                else if (propName.ToLower().IndexOf("date") > -1 || propName.ToLower().IndexOf("dt") > -1)
                                    date = propValue;
                            }
                        }
                    }

                    //if (strBuilder.Length > 0)
                        insertData(fileName, date, model, width, height);
                        //updateData(fileName, strBuilder.ToString());
                }

                theImage.Dispose();
            }
        }

        private void updateData(string fileName, string data)
        {
            System.Data.SqlClient.SqlConnection dbCon;
            System.Data.SqlClient.SqlTransaction TRX;
            libCommon.clsDB objDB = new libCommon.clsDB();

            dbCon = objDB.GetConnection();
            TRX = dbCon.BeginTransaction();
            libMyUtil.clsCmnDB.UPDATE_DB(dbCon, TRX, "tb_EXIFdata", "model", data, "filePath", fileName);
            TRX.Commit();
            dbCon.Close();

        }

        private void insertData(string fileName, string date, string model, string width, string height)
        {
            System.Data.SqlClient.SqlConnection dbCon;
            System.Data.SqlClient.SqlTransaction TRX;
            libCommon.clsDB objDB = new libCommon.clsDB();

            dbCon = objDB.GetConnection();
            TRX = dbCon.BeginTransaction();
            libMyUtil.clsCmnDB.INSERT_DB(dbCon, TRX, "tb_EXIFdata", "filePath|fileName|date|model|width|height", fileName + "|" + Path.GetFileName(fileName) + "|" + date + "|" + model + "|" + width + "|" + height);
            TRX.Commit();
            dbCon.Close();
            
        }

        private string getPropertyName(string id)
        {
            for (int i = 0; i < DT.Rows.Count; i++)
            {
                if (DT.Rows[i][0].ToString().Equals(id))
                {
                    return DT.Rows[i][1].ToString();
                }
            }

            return "";
        }

        private void genPropertyList()
        {
            libCommon.clsUtil objUtil = new libCommon.clsUtil();
            string propertyList = @"Artist|0x013B,
BitsPerSample|0x0102,
Compression|0x0103,
CellWidth|0x0108,
CellHeight|0x0109,
ColorMap|0x0140,
ColorTransferFunction|0x501A,
ChrominanceTable|0x5091,
Copyright|0x8298,
DocumentName|0x010D,
DateTime|0x0132,
DotRange|0x0150,
EquipMake|0x010F,
EquipModel|0x0110,
ExtraSamples|0x0152,
ExifExposureTime|0x829A,
ExifFNumber|0x829D,
ExifIFD|0x8769,
ExifExposureProg|0x8822,
ExifSpectralSense|0x8824,
ExifISOSpeed|0x8827,
ExifOECF|0x8828,
ExifVer|0x9000,
ExifDTOrig|0x9003,
ExifDTDigitized|0x9004,
ExifCompConfig|0x9101,
ExifCompBPP|0x9102,
ExifShutterSpeed|0x9201,
ExifAperture|0x9202,
ExifBrightness|0x9203,
ExifExposureBias|0x9204,
ExifMaxAperture|0x9205,
ExifSubjectDist|0x9206,
ExifMeteringMode|0x9207,
ExifLightSource|0x9208,
ExifFlash|0x9209,
ExifFocalLength|0x920A,
ExifMakerNote|0x927C,
ExifUserComment|0x9286,
ExifDTSubsec|0x9290,
ExifDTOrigSS|0x9291,
ExifDTDigSS|0x9292,
ExifFPXVer|0xA000,
ExifColorSpace|0xA001,
ExifPixXDim|0xA002,
ExifPixYDim|0xA003,
ExifRelatedWav|0xA004,
ExifInterop|0xA005,
ExifFlashEnergy|0xA20B,
ExifSpatialFR|0xA20C,
ExifFocalXRes|0xA20E,
ExifFocalYRes|0xA20F,
ExifFocalResUnit|0xA210,
ExifSubjectLoc|0xA214,
ExifExposureIndex|0xA215,
ExifSensingMethod|0xA217,
ExifFileSource|0xA300,
ExifSceneType|0xA301,
ExifCfaPattern|0xA302,
FillOrder|0x010A,
FreeOffset|0x0120,
FreeByteCounts|0x0121,
FrameDelay|0x5100,
GpsVer|0x0000,
GpsLatitudeRef|0x0001,
GpsLatitude|0x0002,
GpsLongitudeRef|0x0003,
GpsLongitude|0x0004,
GpsAltitudeRef|0x0005,
GpsAltitude|0x0006,
GpsGpsTime|0x0007,
GpsGpsSatellites|0x0008,
GpsGpsStatus|0x0009,
GpsGpsMeasureMode|0x000A,
GpsGpsDop|0x000B,
GpsSpeedRef|0x000C,
GpsSpeed|0x000D,
GpsTrackRef|0x000E,
GpsTrack|0x000F,
GpsImgDirRef|0x0010,
GpsImgDir|0x0011,
GpsMapDatum|0x0012,
GpsDestLatRef|0x0013,
GpsDestLat|0x0014,
GpsDestLongRef|0x0015,
GpsDestLong|0x0016,
GpsDestBearRef|0x0017,
GpsDestBear|0x0018,
GpsDestDistRef|0x0019,
GpsDestDist|0x001A,
GpsIFD|0x8825,
GrayResponseUnit|0x0122,
GrayResponseCurve|0x0123,
Gamma|0x0301,
GridSize|0x5011,
GlobalPalette|0x5102,
HostComputer|0x013C,
HalftoneHints|0x0141,
HalftoneLPI|0x500A,
HalftoneLPIUnit|0x500B,
HalftoneDegree|0x500C,
HalftoneShape|0x500D,
HalftoneMisc|0x500E,
HalftoneScreen|0x500F,
ImageWidth|0x0100,
ImageHeight|0x0101,
ImageDescription|0x010E,
InkSet|0x014C,
InkNames|0x014D,
ICCProfileDescriptor|0x0302,
ImageTitle|0x0320,
IndexBackground|0x5103,
IndexTransparent|0x5104,
ICCProfile|0x8773,
JPEGProc|0x0200,
JPEGInterFormat|0x0201,
JPEGInterLength|0x0202,
JPEGRestartInterval|0x0203,
JPEGLosslessPredictors|0x0205,
JPEGPointTransforms|0x0206,
JPEGQTables|0x0207,
JPEGDCTables|0x0208,
JPEGACTables|0x0209,
JPEGQuality|0x5010,
LuminanceTable|0x5090,
LoopCount|0x5101,
MinSampleValue|0x0118,
MaxSampleValue|0x0119,
NewSubfileType|0x00FE,
NumberOfInks|0x014E,
Orientation|0x0112,
PhotometricInterp|0x0106,
PlanarConfig|0x011C,
PageName|0x011D,
PageNumber|0x0129,
Predictor|0x013D,
PrimaryChromaticities|0x013F,
PrintFlags|0x5005,
PrintFlagsVersion|0x5006,
PrintFlagsCrop|0x5007,
PrintFlagsBleedWidth|0x5008,
PrintFlagsBleedWidthScale|0x5009,
PixelUnit|0x5110,
PixelPerUnitX|0x5111,
PixelPerUnitY|0x5112,
PaletteHistogram|0x5113,
RowsPerStrip|0x0116,
ResolutionUnit|0x0128,
REFBlackWhite|0x0214,
ResolutionXUnit|0x5001,
ResolutionYUnit|0x5002,
ResolutionXLengthUnit|0x5003,
ResolutionYLengthUnit|0x5004,
SubfileType|0x00FF,
StripOffsets|0x0111,
SamplesPerPixel|0x0115,
StripBytesCount|0x0117,
SoftwareUsed|0x0131,
SampleFormat|0x0153,
SMinSampleValue|0x0154,
SMaxSampleValue|0x0155,
SRGBRenderingIntent|0x0303,
ThreshHolding|0x0107,
T4Option|0x0124,
T6Option|0x0125,
TransferFunction|0x012D,
TileWidth|0x0142,
TileLength|0x0143,
TileOffset|0x0144,
TileByteCounts|0x0145,
TargetPrinter|0x0151,
TransferRange|0x0156,
ThumbnailFormat|0x5012,
ThumbnailWidth|0x5013,
ThumbnailHeight|0x5014,
ThumbnailColorDepth|0x5015,
ThumbnailPlanes|0x5016,
ThumbnailRawBytes|0x5017,
ThumbnailSize|0x5018,
ThumbnailCompressedSize|0x5019,
ThumbnailData|0x501B,
ThumbnailImageWidth|0x5020,
ThumbnailImageHeight|0x5021,
ThumbnailBitsPerSample|0x5022,
ThumbnailCompression|0x5023,
ThumbnailPhotometricInterp|0x5024,
ThumbnailImageDescription|0x5025,
ThumbnailEquipMake|0x5026,
ThumbnailEquipModel|0x5027,
ThumbnailStripOffsets|0x5028,
ThumbnailOrientation|0x5029,
ThumbnailSamplesPerPixel|0x502A,
ThumbnailRowsPerStrip|0x502B,
ThumbnailStripBytesCount|0x502C,
ThumbnailResolutionX|0x502D,
ThumbnailResolutionY|0x502E,
ThumbnailPlanarConfig|0x502F,
ThumbnailResolutionUnit|0x5030,
ThumbnailTransferFunction|0x5031,
ThumbnailSoftwareUsed|0x5032,
ThumbnailDateTime|0x5033,
ThumbnailArtist|0x5034,
ThumbnailWhitePoint|0x5035,
ThumbnailPrimaryChromaticities|0x5036,
ThumbnailYCbCrCoefficients|0x5037,
ThumbnailYCbCrSubsampling|0x5038,
ThumbnailYCbCrPositioning|0x5039,
ThumbnailRefBlackWhite|0x503A,
ThumbnailCopyRight|0x503B,
WhitePoint|0x013E,
XResolution|0x011A,
XPosition|0x011E,
YResolution|0x011B,
YPosition|0x011F,
YCbCrCoefficients|0x0211,
YCbCrSubsampling|0x0212,
YCbCrPositioning|0x0213";

            DT = new DataTable();
            string[] property_arr = objUtil.Split(propertyList.Replace("\r\n", ""), ",");

            DT.Columns.Add("ID");
            DT.Columns.Add("Name");

            for (int i = 0; i < property_arr.Length; i++)
            {
                DT.Rows.Add();
                DT.Rows[i][0] = objUtil.Split(property_arr[i], "|")[1];
                DT.Rows[i][1] = objUtil.Split(property_arr[i], "|")[0];
            }
        }
    }
}