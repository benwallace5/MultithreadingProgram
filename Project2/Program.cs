namespace eCommerce
{
    public delegate void priceCutEvent(int price);
    public class myApp
    {
        private static Semaphore empty;
        public static MultiCellBuffer buffer;
        private static ComputerMaker computerMaker;
        static void Main()
        {
            computerMaker = new ComputerMaker();
            empty = new Semaphore(0, 3);  //initializing semaphore with 0, maximum count 2
            buffer = new MultiCellBuffer();
           
            Thread computerMakerThread = new Thread(new ThreadStart(computerMaker.PricingModel));
            Store storeThread = new Store();
            ComputerMaker.priceCut += new priceCutEvent(storeThread.computerOnSale);  //event handler
            Thread[] stores = new Thread[5];

            for (int i = 0; i < 5; i++)  //starting 5 store threads
            {
                stores[i] = new Thread(new ThreadStart(storeThread.storeFunc));
                stores[i].Name = (i + 1).ToString();  //stores are named as follows: store1 - store5
                stores[i].Start();  //start thread running storeFunc
            }
            computerMakerThread.Start();

            empty.Release(3);  //setting semaphore to max value, 2, indicating 2 available resources
            //Main thread exits
        }
        public class ComputerMaker
        {
            static Random random = new Random();
            public static event priceCutEvent priceCut;
            private static int computerPrice = 1300;  //starting computer price $1000
            private static int p = 0;

            public int getPrice()  //get current computerPrice
            {
                return computerPrice;
            }
            public void orderProcessing(Order order)  //check validity of order.cardNo, returns 0 on success, -1 on invalid order
            {
                Thread orderProcessingThread = new Thread(new ThreadStart(() => computerMaker.processOrder(order)));  //starting orderProcessing thread with order as parameter
                orderProcessingThread.Start();
            }
            public int processOrder(Order order)
            {
                int cardNo = order.getCardNo();  //getting card number
                if (cardNo < 4000 || cardNo > 8000)  //validating card number
                {
                    return -1;
                }
                return 0;
            }

            public static void changePrice(int price)
            {
                if (price < computerPrice)  //need to call pricecut event
                {
                    if (priceCut != null)  //there is at least 1 store
                    {
                        priceCut(price);  //call event on all stores
                    }
                    computerPrice = price;  //update price
                    p++;  //increment price cut count
                }
            }
        

            public void PricingModel()
            {
                string encodedOrder;
                Decoder decoder = new Decoder();
                Order order = new Order();

                for(int i = 0; i < 20; i++)
                {
                    Thread.Sleep(500);  //wait 500ms to change price
                    int price = random.Next(900, 1200);  //getting random price 
                    Console.WriteLine("The new price is ${0}", price);
                    empty.WaitOne();  //waiting for available resource
                    encodedOrder = buffer.getOneCell();  //getting encoded order
                    if(encodedOrder != "")
                    {
                      order = decoder.decodeOrder(encodedOrder);  //decoding order to object

                    }
                    computerMaker.orderProcessing(order);
                    Console.WriteLine("Order: {0} has been processed", encodedOrder);
                    empty.Release();
                    ComputerMaker.changePrice(price);  //calling changeprice method on computerMaker to update price
                }
            }
        }
        public class Store
        {
            static Random random = new Random();
            private int quantity;
            private static int currentPrice = 1300;
            private static int name = 1;
            public void storeFunc()  //idea: make quantity a Store class variable. Initialize to 100(or rand(50,100) and adjust with price changes
                                     //then setOneCell only needs to be called in storeFunc and computerOnSale(int) will be responsible for updating quantity.
            {
                Order order = new Order();  //initializing order object
                Encoder encoder = new Encoder();  //initializing encoder object 
                for (int i = 0; i < 10; i++)
                {
                    Thread.Sleep(1000);  //wait 1s to check price
                    int price = computerMaker.getPrice();  //checking price
                    empty.WaitOne();  //trying to enter critical section
                    order.setCardNo(random.Next(4000, 8000));  //random credit card number
                    order.setQuantity(100);  //quantity is initially 100, adjusts with price change
                    order.setSenderId(name);
                    buffer.setOneCell(encoder.encodeOrder(order));
                    empty.Release();  //incrementing empty semaphore
                }
            }

            public void computerOnSale(int price)  //need to send orders into queue, NEED: order/need model to determine quantity
            {
                Order order = new Order();  //initializing order object
                Encoder encoder = new Encoder();  //initializing encoder object  

                Console.WriteLine("Store{0} computers are on sale: as low as ${1} each", Thread.CurrentThread.Name, price);
                int difference = currentPrice - price;  //calculating difference between sale price and current price
                int quatntity = difference % 100;
                int cardNo = random.Next(4000, 8000);  //getting a random credit card number
                order.setCardNo(cardNo);  //setting order attributes
                order.setQuantity(quatntity);
                order.setSenderId(name);
                name++;  //incrementing store id for orders from 1 to 5
                string encodedOrderString = encoder.encodeOrder(order);  //converting order object to string in the form: senderId#cardNo#quantity
               
                empty.WaitOne();  //attempting to enter critical section
                var currentDateTime = DateTime.Now;                //save time stamp
                buffer.setOneCell(encodedOrderString);  //sending order to buffer
                empty.Release();
                
                
            }
        }

        public class Encoder
        {
            public string encodeOrder(Order order)
            {
                string encodedOrder = order.toString();  //toString returns the string senderId#cardNo#quantity
                return encodedOrder;
            }
        }

        public class Decoder
        {
            public Order decodeOrder(string encodedOrder)
            {
                Order decodedOrder = new Order();  //initializing order object to be returned
                string[] orderInfo = encodedOrder.Split("#");  //parsing string senderId#cardNo#quantity
                decodedOrder.setSenderId(int.Parse(orderInfo[0]));  //senderId
                decodedOrder.setCardNo(int.Parse(orderInfo[1]));  //cardNo
                decodedOrder.setQuantity(int.Parse(orderInfo[2]));  //quantity
                return decodedOrder;
            }
        }


        public class Order
        {
            private int senderId;
            private int cardNo;
            private int quantity;

            //getters for private vars
            public int getSenderId()  //return senderId
            {
                return senderId;
            }
            public int getCardNo()  //return cardNo
            {
                return cardNo;
            }
            public int getQuantity()  //return quantity
            {
                return quantity;
            }

            //setters for private vars
            public void setSenderId(int id)  //set senderId
            {
                senderId = id;
            }
            public void setCardNo(int num)  //set cardNo
            {
                cardNo = num;
            }
            public void setQuantity(int num)  //set quantity
            {
                quantity = num;
            }
            public string toString()  //returns string senderId#cardNo#quantity
            {
                string ret;

                ret = senderId.ToString();  //encoding in the form: senderId#cardNo#quantity
                ret += "#";  //using # as delimiter
                ret += cardNo.ToString();
                ret += "#";
                ret += quantity.ToString();
                return ret;  //returning encoded string of order
            }

           
        }  //end of Order class

        public class MultiCellBuffer
        {
            private static string[] buff = {"", "", ""};  //initializing buff with empty strings

            public string getOneCell()  //getOneCell looks for a cell with an order
            {
                string encodedString = "";

                for(int i = 0; i < 3; i++)  //looping through multicell buffer
                {
                    if (buff[i] != "")  //if an order encoding is found
                    {
                        lock (buff[i])  //lock cell[i]
                        {
                            encodedString = buff[i];  //getting order from buffer
                            buff[i] = "";  //emptying cell
                            return encodedString;  //break from for loop and return order in encoded string form
                        }
                      
                    }
                }
                return encodedString;  //returning order in encoded string form

            }

            public void setOneCell(string encodedString)  //set one cell takes an encoded order and attempts to insert it into buffer
            {
                Decoder decoder = new Decoder();  //initializing decoder object
                Order decodedOrder = decoder.decodeOrder(encodedString);  //decoding encoded string to get store id

                    for (int i = 0; i < 3; i++)  //looking for an empty cell
                    {
                        if (buff[i] == "")  //if buffer cell is empty
                        {
                            lock (buff[i])  //lock cell[i]
                            {
                              buff[i] = encodedString;  //setting cell to encoded order string
                              Console.WriteLine("Order {0} has been sent", encodedString);
                              Thread.Sleep(1000);  //sleeping to increase cell usage
                              return;  //break from loop
                           }
                        }
                    }
                
            }
        }
    } 
}
