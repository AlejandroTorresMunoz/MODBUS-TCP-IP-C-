using System;
using System.Net;
using System.Net.Sockets;
using UnityEngine;


namespace TCPSokets //With the scope to create our own library
{
    public struct DiscreteCoilsValues //Structure with the values of the discrete coils that have been read
    {
        public int startingDirection; //PLC's direction of the first logic value in the following array  
        public bool[] arrayCoilsValues; //Boolean array with the values of the registers
        public DiscreteCoilsValues(int startingDirectionValue, bool[] arrayCoilsValuesValue)
        {
            //Constructor of the structure
            startingDirection = startingDirectionValue;
            arrayCoilsValues = arrayCoilsValuesValue; 
        }
    }

    public struct WordCoilsValues //Structure with the values of the holding registers that have been read
    {
        public int startingDirection; //PLC's direction of the first word value in the following array 
        public UInt16[] arrayWordsValues; //UInt16 array with the values of the registers
        public WordCoilsValues(int startingDirectionValue, UInt16[] arrayWordsValuesValue)
        {
            //Constructor of the structure
            startingDirection = startingDirectionValue;
            arrayWordsValues = arrayWordsValuesValue;
        }
    }
    public class ClientTCPClass //Main class
    {
        private string PLCDirection; //IP Address of the PLC, as string
        private int Port; //Communication Port
        private IPAddress PLCIP; //IP Address of the PLC, as IPAddress object
        private IPEndPoint EndPoint; //IP Endpoint
        private Int32 TimeOut; //Timeout to send and receive
        private Socket s; //Socket fot the communication


        public ClientTCPClass(string direction_in_string, int PortValue, int TimeOutValue)
        {
            //Constructor of the class
            this.PLCDirection = direction_in_string;
            this.Port = PortValue;
            this.PLCIP = IPAddress.Parse(this.PLCDirection);
            this.EndPoint = new IPEndPoint(this.PLCIP,this.Port);
            this.TimeOut = TimeOutValue;
        }
            
        public bool SocketCreation()
        {
            //Function to create the socket
            Socket tempSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp); //Auxiliar socket to establish the communication
            //AddressFamily.InterNetwork --> For IPv4 addressing
            //SocketType.Stream --> Type of the socket. Enables communication in both directions. Needed for the communication in TCP
            //ProtocolType.TCP --> TCP protocol use

            try
            {
                tempSocket.Connect(this.EndPoint); //Connection to the endpoint

            }
            catch(SocketException e)
            {
                //In case of error trying to connect
                Debug.LogError(e.Message + " Error code: " + e.ErrorCode);
            }
            if (tempSocket.Connected)
            {
                //In the case the connection was done
                s = tempSocket;
                s.ReceiveBufferSize = 256; //256 as RX buffer size, which is the max.length of a TCP message
                s.SendBufferSize = 256; //256 as TX buffer size, which is the max.length of a TCP message
                s.ReceiveTimeout = this.TimeOut;
                s.SendTimeout = this.TimeOut;
                return true;
            }
            else
            {
                Debug.LogError("Error during the TCP connection");
                return false;
            }
        }

        public void SocketSendDiscreteCoils(byte slave_Address, UInt16 offSetDirection, UInt16 numberOfRegisters)
        {
            //Function to send the needed data to read from the discrete coils
            //--------------------------------------------------//
            //Construction of the message
            byte[] startingDirection = BitConverter.GetBytes(offSetDirection);
            byte[] numRegister = BitConverter.GetBytes(numberOfRegisters);
            byte[] message = new byte[12];
            message[0] = 0; //Higher byte for the transaction ID
            message[1] = 0; //Lower byte for the transaction ID
            message[2] = 0; //Higher byte for the protocol ID
            message[3] = 0; //Lower byte for the protocol ID --> Always 0 fot the TCP Modbus Ethernet
            message[4] = 0; //Higher byte for the remaining length of the message, in bytes
            message[5] = 6; //Lower byte for the remaining length of the message, in bytes
            message[6] = slave_Address; //Byte to indicate the slave's address
            message[7] = 1; //Byte to indicate the function -> Read from discrete coils
            message[8] = startingDirection[1]; //Higher byte for the offset direction of the coils that want to be read
            message[9] = startingDirection[0]; //Lower byte for the offset direction of the coils that want to be read
            message[10] = numRegister[1]; //Higher byte to indicate the number of registers we want to read
            message[11] = numRegister[0]; //Lower byte to indicate the number of registers we want to read
            //--------------------------------------------------//
            //Send the message
            try
            {
                this.s.Send(message, message.Length, SocketFlags.None);
            }
            catch (SocketException e)
            {
                //In case of overtiming
                Debug.LogError(e.Message + " Error code: " + e.ErrorCode);
            }
        }

        public void SocketSendWordCoils(byte slave_Address, UInt16 offSetDirection, UInt16 numberOfRegisters)
        {
            //Function to send the needed data to read from the word coils
            //--------------------------------------------------//
            //Construction of the message
            byte[] startingDirection = BitConverter.GetBytes(offSetDirection);
            byte[] numRegister = BitConverter.GetBytes(numberOfRegisters);
            byte[] message = new byte[12];
            message[0] = 0; //Higher byte for the transaction ID
            message[1] = 0; //Lower byte for the transaction ID
            message[2] = 0; //Higher byte for the protocol ID
            message[3] = 0; //Lower byte for the protocol ID --> Always 0 fot the TCP Modbus Ethernet
            message[4] = 0; //Higher byte for the remaining length of the message, in bytes
            message[5] = 6; //Lower byte for the remaining length of the message, in bytes
            message[6] = slave_Address; //Byte to indicate the slave's address
            message[7] = 3; //Byte to indicate the function -> Read from discrete coils
            message[8] = startingDirection[1]; //Higher byte for the offset direction of the coils that want to be read
            message[9] = startingDirection[0]; //Lower byte for the offset direction of the coils that want to be read
            message[10] = numRegister[1]; //Higher byte to indicate the number of registers we want to read
            message[11] = numRegister[0]; //Lower byte to indicate the number of registers we want to read
            //--------------------------------------------------//
            //Send the message
            try
            {
                this.s.Send(message, message.Length, SocketFlags.None);
            }
            catch (SocketException e)
            {
                //In case of overtiming
                Debug.LogError(e.Message+" Error code: " +e.ErrorCode);
            }

        }

        public bool[] ReadRxBufferDiscreteCoils()
        {
            //Function to read from the RX buffer, intended for the discrete coils
            byte[] RXXData = new byte[255]; //Data to be stored
            try
            {
                this.s.Receive(RXXData, RXXData.Length, SocketFlags.None);
            }
            catch (SocketException e)
            {
                //In case of overtiming
                Debug.LogError(e.Message + " Error code: " + e.ErrorCode);
            }

            if(RXXData[7] == Convert.ToByte(0x01+0x80))
            {
                //In the case of error response
                Debug.LogError("Error in the response, executing function 1");
                switch(RXXData[8])
                {
                    case 0x01:
                        Debug.LogError("Function code not supported");
                        break;
                    case 0x02:
                        Debug.LogError("Starting address not supported or starting address+quantity of registers not supported");
                        break;
                    case 0x03:
                        Debug.LogError("Quantity of outputs not supported");
                        break;
                    case 0x04:
                        Debug.LogError("Cannot read the values");
                        break;
                    default:
                        Debug.LogError("Unknown error");
                        break;
                }
                return null;
            }
            else
            {        
                UInt16 num_bytes_data = Convert.ToUInt16(RXXData[8]); //Number of bytes of data

                var data_in_byte = new byte[num_bytes_data]; //Data bytes
                Buffer.BlockCopy(RXXData, 9,data_in_byte,0,data_in_byte.Length); //Copy those data bytes

                var data_in_bits = new bool[data_in_byte.Length*8]; //Array with the values of the registers

                //Store the data of the discrete coils in the variable "data_in_bits"
                for(int i=0;i<data_in_byte.Length;i++)
                {
                    for(int j=0;j<8;j++)
                    {
                        data_in_bits[j + (i*8)] = (data_in_byte[i] & (1 << j)) == 0 ? false : true;
                    }
                }
                //Debug.Log("Discrete RX buffer read successfully");
                return data_in_bits;
            }
        }
        public UInt16[] ReadRxBufferWordCoils()
        {
            //Function to read from the RX buffer, intended for the word coils
            byte[] RXXData = new byte[255];
            try
            {
                this.s.Receive(RXXData, RXXData.Length, SocketFlags.None);
            }
            catch (SocketException e)
            {
                //In case of overtiming
                Debug.LogError(e.Message + " Error code: " + e.ErrorCode);
            }


            if(RXXData[7] == 0x83)
            {
                Debug.LogError("Error reading the values of the coils");
                switch(RXXData[8])
                {
                    //In the case of error response
                    case 0x01:
                        Debug.LogError("Function code not supported");
                        break;
                    case 0x02:
                        Debug.LogError("Starting address not supported or starting address+quantity of registers not supported");
                        break;
                    case 0x03:
                        Debug.LogError("Quantity of registers not supported");
                        break;
                    case 0x04:
                        Debug.LogError("Cannot read the values");
                        break;
                    default:
                        Debug.LogError("Unknown error");
                        break;
                }
            }
            UInt16 num_bytes_data = Convert.ToUInt16(RXXData[8]); //Number of bytes of data

            var data_bytes = new byte[num_bytes_data]; //Array with only the data bytes
            Buffer.BlockCopy(RXXData, 9, data_bytes, 0, data_bytes.Length); //Copy those bytes

            var data_word = new UInt16[num_bytes_data / 2]; //Value of the registers in the proper format, in words of 16 bits
            //Conversion from array to word
            for (int i = 0; i < data_word.Length; i++)
            {
                //Note: The first byte of the pair is composed by the 8 bits of highest value  
                data_word[i] = Convert.ToUInt16(Convert.ToUInt16(data_bytes[i * 2] << 8) + (Convert.ToUInt16(data_bytes[(i * 2) + 1])));
            }
            return data_word;
        }

        public DiscreteCoilsValues ReadValuesDiscreteCoils(byte slave_Address, UInt16 offSetDirection, UInt16 numberOfRegisters)
        {
            //Function to send the request of reading discrete coils values and return these values
            SocketSendDiscreteCoils(slave_Address, offSetDirection, numberOfRegisters); //Send the request
            bool[] data_read = ReadRxBufferDiscreteCoils(); //Extract the data
            var registers_data = new bool[numberOfRegisters]; //Boolean array to return
            Buffer.BlockCopy(data_read, 0, registers_data, 0, registers_data.Length); //Copy the byte's values
            DiscreteCoilsValues struct_to_return = new DiscreteCoilsValues();
            struct_to_return.startingDirection = offSetDirection; //Direction of the first value returned
            struct_to_return.arrayCoilsValues = registers_data; //Coils values
            return struct_to_return;
        }
        public WordCoilsValues ReadValuesWordCoils(byte slave_Address, UInt16 offSetDirection, UInt16 numberOfRegisters)
        {
            SocketSendWordCoils(slave_Address, offSetDirection, numberOfRegisters); //Send the request
            UInt16[] data_read = ReadRxBufferWordCoils(); //Extract the information from the PLC's answer

            var registers_value = new UInt16[numberOfRegisters]; //Array to be returned in the structure, with the proper length

            registers_value = data_read; // Buffer.BlockCopy didn't work properly. We get the array value
            //Buffer.BlockCopy(valores_leidos, 0, datos_registros, 0, datos_registros.Length+1); //Copiamos los bytes en datos
            
            WordCoilsValues struct_to_return = new WordCoilsValues(); //Initialization of the structure

            struct_to_return.startingDirection = offSetDirection; //Direction of the first value returned
            struct_to_return.arrayWordsValues = registers_value; //Words values

            return struct_to_return;
        }
        public void WriteOnDiscreteCoils(byte slave_Address, UInt16 offSetDirection, bool[] ValueOfTheCoils)
        {
            //Function to send the needed data to write on the discrete coils
            //--------------------------------------------------//
            //Construction of the message
            byte[] startingDirection = BitConverter.GetBytes(offSetDirection);

            int bytes = ValueOfTheCoils.Length / 8;
            if ((ValueOfTheCoils.Length % 8) != 0) bytes++; //Length of the byte's array

            byte[] arr1 = new byte[bytes]; //Byte's array
            int bitIndex = 0, byteIndex = 0;
            //Construction of the array
            for (int i = 0; i < ValueOfTheCoils.Length; i++)
            {
                //Fullfill the thw bytes of data to be sent bit per bit
                if (ValueOfTheCoils[i])
                {
                    arr1[byteIndex] |= (byte)(((byte)1) << bitIndex);
                }
                bitIndex++;
                if (bitIndex == 8)
                {
                    //Increse the index to fullfill the next byte to be sent
                    bitIndex = 0;
                    byteIndex++;
                }
            }

            //The construction of the array follows the next criteria
            /*
             * (First byte)             - (Second byte)
             * (R7-R6-R5-R4-R3-R2-R1-R0)-(R17-R16-R15-R14-R13-R12-R11-R10-R9-R8)
             * Rx is the register number x
            */
            byte[] message = new byte[13 + arr1.Length];
            message[0] = 0; //Higher byte for the transaction ID
            message[1] = 0; //Lower byte for the transaction ID
            message[2] = 0; //Higher byte for the protocol ID
            message[3] = 0; //Lower byte for the protocol ID --> Always 0 fot the TCP Modbus Ethernet
            message[4] = 0; //Higher byte for the remaining length of the message, in bytes
            message[5] = Convert.ToByte(message.Length - 6); //Lower byte for the remaining length of the message, in bytes
            message[6] = slave_Address; //Byte to indicate the slave's address
            message[7] = 15; //Byte to indicate the function -> Write on discrete coils
            message[8] = startingDirection[1]; //Higher byte for the offset direction of the coils that want to be write
            message[9] = startingDirection[0]; //Lower byte for the offset direction of the coils that want to be write
            message[10] = 0; //Higher value of the number of coils to be written
            message[11] = Convert.ToByte(ValueOfTheCoils.Length); //Lower value of the number of coils to be written
            message[12] = Convert.ToByte(arr1.Length); //Number of data bytes

            Buffer.BlockCopy(arr1, 0, message, 13, arr1.Length); //Add the data bytes

            //--------------------------------------------------//
            //Send the message
            try
            {
                this.s.Send(message, message.Length, SocketFlags.None);
            }
            catch (SocketException e)
            {
                //In case of overtiming
                Debug.LogError(e.Message + " Error code: " + e.ErrorCode);
            }
        }

        public void WriteOnWordCoils(byte slave_Address, UInt16 offSetDirection, UInt16[] ValueOfTheRegisters)
        {
            //Function to send the needed data to write on the holdding/word registers
            //--------------------------------------------------//
            //Construction of the message

            var arr1 = new byte[ValueOfTheRegisters.Length * 2]; //Array of data bytes to send
            int byteIndex = 0;
            UInt16 mask_1 = 0xFF00; //Mask 1 to apply
            UInt16 mask_2 = 0x00FF; //Mask 2 to apply
            for (int i = 0; i < ValueOfTheRegisters.Length; i++)
            {
                //Extract each one of the data bytes from the UInt16 array received to be sent
                arr1[byteIndex] = BitConverter.GetBytes(ValueOfTheRegisters[i] & mask_1)[1]; //Extraction the most significative byte
                byteIndex++;
                arr1[byteIndex] = BitConverter.GetBytes(ValueOfTheRegisters[i] & mask_2)[0]; //Extraction the least significative byte 
                byteIndex++;
            }

            byte[] startingDirection = BitConverter.GetBytes(offSetDirection); //Convert the offset direction to the proper form

            byte[] message = new byte[13 + arr1.Length];
            message[0] = 0; //Higher byte for the transaction ID
            message[1] = 0; //Lower byte for the transaction ID
            message[2] = 0; //Higher byte for the protocol ID
            message[3] = 0; //Lower byte for the protocol ID --> Always 0 fot the TCP Modbus Ethernet
            message[4] = 0; //Higher byte for the remaining length of the message, in bytes
            message[5] = Convert.ToByte(message.Length - 6); //Lower byte for the remaining length of the message, in bytes
            message[6] = slave_Address; //Byte to indicate the slave's address
            message[7] = 16; //Byte to indicate the function -> Write on discrete coils
            message[8] = startingDirection[1]; //Higher byte for the offset direction of the coils that want to be write
            message[9] = startingDirection[0]; //Lower byte for the offset direction of the coils that want to be write
            message[10] = 0; //Higher value of the number of holding registers to be written
            message[11] = Convert.ToByte(ValueOfTheRegisters.Length); //Lower value of the number of holding registers to be written
            message[12] = Convert.ToByte(arr1.Length); //Number of data bytes

            Buffer.BlockCopy(arr1, 0, message, 13, arr1.Length);

            //--------------------------------------------------//
            //Send the message
            try
            {
                this.s.Send(message, message.Length, SocketFlags.None);
            }
            catch (SocketException e)
            {
                //In case of overtiming
                Debug.LogError(e.Message + " Error code: " + e.ErrorCode);
            }
        }

        public bool ReadResponseAfterWritting(byte function_code, UInt16 offSetDirection)
        {
            //Function to check if the writing have been done successfully
            byte[] RXXData = new byte[255];
            this.s.Receive(RXXData, RXXData.Length, SocketFlags.None); //Read from the socket
            UInt16 starting_address_check = (UInt16)( (RXXData[9]) | (RXXData[8]) << 8); //Extract the starting address
            if(RXXData[7] == function_code && starting_address_check == offSetDirection )
            {
                //In the case there's been no problems
                return true;
            }
            else
            {
                //In the case of error, check why it happened
                switch(function_code)
                {
                    case 0x0F:
                        //Write on discrete coils
                        if(RXXData[7] == 0x8F)
                        {
                            switch(RXXData[8])
                            {
                                case 0x01:
                                    Debug.LogError("Function code not supported");
                                    break;
                                case 0x02:
                                    Debug.LogError("Starting address+quantity of outputs not supported");
                                    break;
                                case 0x03:
                                    Debug.LogError("Quantity of outputs not supported");
                                    break;
                                case 0x04:
                                    Debug.LogError("Cannot write");
                                    break;
                                default:
                                    Debug.LogError("Unknown code error");
                                    break;
                            }
                        }
                        else
                        {
                            Debug.LogError("Error code unknown");
                        }
                        break;
                    case 0x10:
                        //Write on holding registers coils
                        if(RXXData[7] == 0x90)
                        {
                            switch(RXXData[8])
                            {
                                case 0x01:
                                    Debug.LogError("Function code not supported");
                                    break;
                                case 0x02:
                                    Debug.LogError("Starting address+quantity of outputs not supported");
                                    break;
                                case 0x03:
                                    Debug.LogError("Quantity of outputs not supported");
                                    break;
                                case 0x04:
                                    Debug.LogError("Cannot write");
                                    break;
                                default:
                                    Debug.LogError("Unknown code error");
                                    break;
                            }
                        }
                        else
                        {
                            Debug.LogError("Error code unknown");
                        }
                        break;
                    default:
                        Debug.LogError("Function code send it is not supported");
                        break;
                }
                return false;
            }
        }


        public void WriteDiscreteCoils(byte slave_Address, UInt16 offSetDirection, bool[] coilsValue)
        {
            //Function to do the whole process to write on discrete coils
            WriteOnDiscreteCoils(slave_Address, offSetDirection, coilsValue); //Write on the discrete coils
            byte function_code = 0x0F;
            if(ReadResponseAfterWritting(function_code, offSetDirection))
            {
                //In the case all was done properly
                //Debug.Log("Write on the coils done successfully");
            }
            else
            {
                //In case there's an error
                Debug.LogError("Error trying to write on the coils");
            }
        }

        public void WriteWordCoils(byte slave_Address, UInt16 offSetDirection, UInt16[] ValueOfTheRegisters)
        {
            //Function to do the whole process to write on word coils
            WriteOnWordCoils(slave_Address, offSetDirection, ValueOfTheRegisters);
            byte function_code = 0x10;
            if(ReadResponseAfterWritting(function_code, offSetDirection))
            {
                //In the case all was done properly
                //Debug.Log("Write on the coils done successfully");
            }
            else
            {
                //In case there's an error
                Debug.LogError("Error trying to write on the coils");
            }
        }


    }
}
