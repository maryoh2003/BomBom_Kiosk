﻿using BomBom_Kiosk.Model;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace BomBom_Kiosk.Service
{
    public class DBManager
    {
        private MySqlCommand cmd;

        public bool ConnectDB()
        {
            MySqlConnection conn = new MySqlConnection(App.connStr);

            try
            {
                conn.Open();
                cmd = conn.CreateCommand();

                return true;
            }
            catch
            {
                MessageBox.Show("DB 연결이 되지 않았습니다.");
                cmd =  null;

                return false;
            }
        }

        public List<Drink> GetDrinks()
        {
            cmd.CommandText = "SELECT * FROM menu";

            using (MySqlDataReader reader = cmd.ExecuteReader())
            {
                List<Drink> drinks = new List<Drink>();

                while (reader.Read())
                {
                    Drink drink = new Drink();
                    drink.Idx = int.Parse(reader["idx"].ToString());
                    drink.Name = reader["name"].ToString();
                    drink.Price = int.Parse(reader["price"].ToString());
                    drink.Image = reader["image"].ToString();
                    drink.Category = (ECategory)int.Parse(reader["type"].ToString());

                    drinks.Add(drink);
                }

                return drinks;
            }
        }

        public List<MemberModel> GetMembers()
        {
            cmd.CommandText = "SELECT * FROM member";

            using (MySqlDataReader reader = cmd.ExecuteReader())
            {
                List<MemberModel> members = new List<MemberModel>();

                while (reader.Read())
                {
                    MemberModel member = new MemberModel();
                    member.Idx = int.Parse(reader["idx"].ToString());
                    member.Name = reader["name"].ToString();
                    member.Code = reader["code"].ToString();
                    member.Id = reader["id"].ToString();
                    member.Pw = reader["pw"].ToString();

                    members.Add(member);
                }
                
                return members;
            }
        }

        public List<OrderedItem> GetOrderedItem(int menuIdx)
        {
            cmd.CommandText = $"SELECT * FROM order_item WHERE menu_idx = {menuIdx}";
            using (MySqlDataReader reader = cmd.ExecuteReader())
            {
                List<OrderedItem> orderedItems = new List<OrderedItem>();

                while (reader.Read())
                {
                    OrderedItem orderedItem = new OrderedItem();
                    orderedItem.Idx = int.Parse(reader["idx"].ToString());
                    orderedItem.Menu.Idx = int.Parse(reader["menu_idx"].ToString());
                    orderedItem.Member.Idx = int.Parse(reader["member_idx"].ToString());
                    orderedItem.Count = int.Parse(reader["count"].ToString());
                    orderedItem.Seat = int.Parse(reader["seat"].ToString());
                    orderedItem.OrderCode = reader["order_code"].ToString();
                    if (int.Parse(reader["place"].ToString()) == 0)
                    {
                        orderedItem.Place = EOrderPlace.InShop;
                    }
                    else if (int.Parse(reader["place"].ToString()) == 1)
                    {
                        orderedItem.Place = EOrderPlace.Packing;
                    }
                    if (int.Parse(reader["payment_type"].ToString()) == 0)
                    {
                        orderedItem.Type = EOrderType.Cash;
                    }
                    else if (int.Parse(reader["payment_type"].ToString()) == 1)
                    {
                        orderedItem.Type = EOrderType.Card;
                    }
                    orderedItem.OrderDateTime = DateTime.Parse(reader["order_code"].ToString());
                    orderedItems.Add(orderedItem);
                }
                return orderedItems;
            }
        }

        public void Payment()
        {
            foreach (var item in App.orderViewModel.OrderList)
            {
                AddOrderItem(item);
            }

            int index = GetLastIndex("order_number");
            string orderNumber = GetOrderNumber(index - 1);

            cmd.CommandText = "INSERT INTO order_number (idx, order_number) " +
                             $"VALUES ({index}, " +
                             $"{orderNumber})";

            cmd.ExecuteNonQuery();

            while (orderNumber.Length < 3)
            {
                orderNumber = "0" + orderNumber;
            }

            App.paymentViewModel.OrderNumber = orderNumber;
        }

        private void AddOrderItem(OrderedDrink orderedDrink)
        {
            OrderData orderInfo = App.paymentViewModel.OrderInfo;
            int index = GetLastIndex("order_item");

            if (orderInfo.Place == EOrderPlace.InShop)
            {
                cmd.CommandText = "INSERT INTO order_item (idx, menu_idx, member_idx, count, seat, order_code, place, payment_type, order_date_time) " +
                                 $"VALUES ({index}," +
                                 $"{orderedDrink.MenuIdx}, " +
                                 $"{orderInfo.MemberIdx}, " +
                                 $"{orderedDrink.Count}, " +
                                 $"{orderInfo.Table}, " +
                                 $"'{orderInfo.Code}', " +
                                 $"{(int)orderInfo.Place}, " +
                                 $"{(int)orderInfo.Type}, " +
                                 $"'{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}')";

                App.paymentViewModel.Tables.Where(x => x.Number == orderInfo.Table).FirstOrDefault().IsUsing = true;
            }
            else
            {
                cmd.CommandText = "INSERT INTO order_item (idx, menu_idx, member_idx, count, order_code, place, payment_type, order_date_time)" +
                                 $"VALUES ({index}," +
                                 $"{orderedDrink.MenuIdx}, " +
                                 $"{orderInfo.MemberIdx}, " +
                                 $"{orderedDrink.Count}, " +
                                 $"{orderInfo.Table}, " +
                                 $"'{orderInfo.Code}', " +
                                 $"{(int)orderInfo.Place}, " +
                                 $"{(int)orderInfo.Type}, " +
                                 $"'{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}')";
            }

            cmd.ExecuteNonQuery();
        }

        private int GetLastIndex(string tableName)
        {
            int index = 0;

            cmd.CommandText = $"SELECT idx FROM {tableName}";

            using (MySqlDataReader reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    if (index < int.Parse(reader["idx"].ToString()))
                    {
                        index = int.Parse(reader["idx"].ToString());
                    }
                }

                return index + 1;
            }
        }

        private string GetOrderNumber(int index)
        {
            int orderNumber = 0;

            cmd.CommandText = "SELECT order_number FROM order_number WHERE idx=" + index;

            using (MySqlDataReader reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    orderNumber = int.Parse(reader["order_number"].ToString()) + 1;
                }
            }

            if (orderNumber == 100)
            {
                orderNumber = 1;
            }

            return orderNumber.ToString();
        }

        public void SaveTime(TimeSpan usedTime)
        {
            if (cmd == null)
            {
                return;
            }

            DateTime totalTime = new DateTime();
            cmd.CommandText = "SELECT * FROM time";

            using (MySqlDataReader reader = cmd.ExecuteReader())
            {

                while (reader.Read())
                {
                    totalTime = DateTime.Parse(reader["time"].ToString()) + usedTime;
                }
            }

            cmd.CommandText = "UPDATE time " +
                             $"SET time='{totalTime.ToString("yyyy-MM-dd HH:hh:ss")}'";
            cmd.ExecuteNonQuery();
        }
    }
}
