using MovieViews.Models;
using System;
using System.IO;
using System.Collections.ObjectModel;
using Newtonsoft.Json.Linq;
using System.Net;
using System.Windows.Input;
using System.Data;
using System.Data.SqlClient;
using System.Xml;
using System.Xml.Linq;
using System.Linq;

namespace MovieViews.ViewModels
{

    public class MainWindowViewModel : Notifier
    {
        /// <summary>
        /// DB연결 부분
        /// </summary>
        #region DbConnection
        DataSet ds;
        string MvJson;
        string constr = @"Data Source=(localdb)\MSSQLLocalDB;Initial Catalog=MvvmMovieViews.Data;Integrated Security=True;Connect Timeout=30;Encrypt=False;TrustServerCertificate=False;ApplicationIntent=ReadWrite;MultiSubnetFailover=False";
        #endregion

        /// <summary>
        /// 초기 날짜 설정 (오늘의 전날)
        /// </summary>
        static DateTime now = DateTime.Now;
        static string Date = now.AddDays(-1).ToString("yyyyMMdd");

        static string ApiKey = "";

        /// <summary>
        /// 영화 ApiKey 추출부분
        /// </summary>
        /// <returns></returns>
        static string RequestApiKey()
        {
            string keyuri = @"..\..\ApiKey.xml";

            try
            {
                XmlDocument xml = new();
                xml.Load(keyuri);

                XmlNodeList nodeList = xml.SelectNodes("/resources");
                ApiKey = nodeList[0].InnerText;
            }
            catch (ArgumentException ex)
            {
                Console.WriteLine(ex.Message);
            }
            return ApiKey;
        }
        
        /// <summary>
        /// API 연동주소 생성 메소드
        /// </summary>
        /// <param name="Date"></param>
        /// <returns></returns>
        static string RequestURL(string Date)
        {
            ApiKey = RequestApiKey();

            string result = string.Format("https://kobis.or.kr/kobisopenapi/webservice/rest/boxoffice/searchDailyBoxOfficeList.json?key={0}&targetDt={1}", ApiKey, Date);
            return result;
        }

        MovieModel movie = null;

        /// <summary>
        /// 메인 윈도우 시동시 작동하는 것??
        /// </summary>
        public MainWindowViewModel()
        {
            movie = new MovieModel();
            AddMovie();

            CuDate = ChangeDate(Date);
        }

        /// <summary>
        /// ~~~년 ~~월 ~~일 박스오피스 출력 감시 기능
        /// </summary>
        private string _cuDate;

        public string CuDate
        {
            get { return _cuDate; }
            set
            {
                _cuDate = value;
                OnPropertyChanged("CuDate");
            }
        }

        /// <summary>
        /// 영화 콜렉션 출력 감시 기능
        /// </summary>
        ObservableCollection<MovieModel> movies = null;
        public ObservableCollection<MovieModel> Movies
        {
            get
            {
                if (movies == null)
                {
                    movies = new ObservableCollection<MovieModel>();
                }
                return movies;
            }
            set
            {
                movies = value;
                OnPropertyChanged("Movies");
            }
        }

        /// <summary>
        /// DB에 해당 날짜의 영화 정보가 있는지 보고 없으면 추가시켜주며 있으면 이미 있는 정보를 출력해주는 기능
        /// </summary>
        public void AddMovie()
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(constr))
                {
                    conn.Open();

                    /// <summary>
                    /// Linq 사용해서 DB와 연결 >> .ToList() 안하면 쌩 쿼리문이 날아옴
                    /// </summary>
                    using (var context = new MoviesViewEntities())
                    {
                        /// <summary>
                        /// SQL 버전 > 꺼내쓰기 log[0].log 요런식으로 사용
                        /// </summary>
                        var log = from x in context.Movies_Logs
                                  where x.date == Date
                                  select x;


                        /// <summary>
                        /// 람다 버전 > 꺼내는 방법 동일
                        /// </summary>
                        // var logs = context.Movies_Logs
                        //     .Where(x => x.date == Date)
                        //     .ToList();

                        /// <summary>
                        /// Count()를 쓰려면 ToList() 쓰면 안됨
                        /// </summary>
                        if (log.Count() == 0)
                        {
                            WebRequest request = WebRequest.Create(RequestURL(Date));
                            request.Method = "GET";
                            request.ContentType = "application/json";

                            using (WebResponse response = request.GetResponse())
                            using (Stream stream = response.GetResponseStream())
                            using (StreamReader reader = new StreamReader(stream))
                            {
                                string data = reader.ReadToEnd();
                                var obj = JObject.Parse(data);

                                MvJson = Convert.ToString(obj);

                                var Movies_Log = new Movies_Logs();
                                Movies_Log.date = Date;
                                Movies_Log.log = MvJson;

                                context.Movies_Logs.Add(Movies_Log);
                                context.SaveChanges();

                                var boxOfficeResult = obj["boxOfficeResult"];
                                var dailyBoxOfficeList = boxOfficeResult["dailyBoxOfficeList"];

                                foreach (var item in dailyBoxOfficeList)
                                {
                                    long coTEarn = Convert.ToInt64(item["salesAcc"]);
                                    double coPct = Convert.ToDouble(item["salesShare"]);
                                    long coTAud = Convert.ToInt64(item["audiAcc"]);

                                    Movies.Add(new MovieModel()
                                    {
                                        Num = (int)item["rank"],
                                        Name = (string)item["movieNm"],
                                        ODate = (string)item["openDt"],
                                        TEarn = coTEarn,
                                        Pct = coPct,
                                        TAud = coTAud
                                    });
                                }
                            }
                        }
                        else
                        {
                            var data = log.ToList()[0].log;
                            var obj = JObject.Parse((string)data);

                            var boxOfficeResult = obj["boxOfficeResult"];
                            var dailyBoxOfficeList = boxOfficeResult["dailyBoxOfficeList"];

                            foreach (var item in dailyBoxOfficeList)
                            {
                                long coTEarn = Convert.ToInt64(item["salesAcc"]);
                                double coPct = Convert.ToDouble(item["salesShare"]);
                                long coTAud = Convert.ToInt64(item["audiAcc"]);

                                Movies.Add(new MovieModel()
                                {
                                    Num = (int)item["rank"],
                                    Name = (string)item["movieNm"],
                                    ODate = (string)item["openDt"],
                                    TEarn = coTEarn,
                                    Pct = coPct,
                                    TAud = coTAud
                                });
                            }
                        }
                    }

                    #region SQL 버전
                    //using (SqlDataAdapter adapter = new SqlDataAdapter("SELECT * FROM [dbo].[Movies.Logs] WHERE date = @_date", conn))
                    //{
                    //    adapter.SelectCommand.Parameters.AddWithValue("@_date", Date);
                    //    ds = new DataSet();
                    //    adapter.Fill(ds);

                    //    if (ds.Tables[0].Rows.Count == 0)
                    //    {
                    //        WebRequest request = WebRequest.Create(RequestURL(Date));
                    //        request.Method = "GET";
                    //        request.ContentType = "application/json";

                    //        using (WebResponse response = request.GetResponse())
                    //        using (Stream stream = response.GetResponseStream())
                    //        using (StreamReader reader = new StreamReader(stream))
                    //        {
                    //            string data = reader.ReadToEnd();
                    //            var obj = JObject.Parse(data);

                    //            MvJson = Convert.ToString(obj);
                    //            using (SqlCommand cmd = new SqlCommand("INSERT INTO [dbo].[Movies.Logs] VALUES (@_date, @_log)", conn))
                    //            {
                    //                cmd.Parameters.AddWithValue("@_date", Date);
                    //                cmd.Parameters.AddWithValue("@_log", MvJson);
                    //                cmd.ExecuteNonQuery();
                    //            }

                    //            conn.Close();

                    //            var boxOfficeResult = obj["boxOfficeResult"];
                    //            var dailyBoxOfficeList = boxOfficeResult["dailyBoxOfficeList"];

                    //            foreach (var item in dailyBoxOfficeList)
                    //            {
                    //                long coTEarn = Convert.ToInt64(item["salesAcc"]);
                    //                double coPct = Convert.ToDouble(item["salesShare"]);
                    //                long coTAud = Convert.ToInt64(item["audiAcc"]);

                    //                Movies.Add(new MovieModel()
                    //                {
                    //                    Num = (int)item["rank"],
                    //                    Name = (string)item["movieNm"],
                    //                    ODate = (string)item["openDt"],
                    //                    TEarn = coTEarn,
                    //                    Pct = coPct,
                    //                    TAud = coTAud
                    //                });
                    //            }
                    //        }
                    //    } else
                    //    {
                    //        conn.Close();

                    //        DataTable dt = ds.Tables[0];

                    //        var data = dt.Rows[0][2];
                    //        var obj = JObject.Parse((string)data);

                    //        var boxOfficeResult = obj["boxOfficeResult"];
                    //        var dailyBoxOfficeList = boxOfficeResult["dailyBoxOfficeList"];

                    //        foreach (var item in dailyBoxOfficeList)
                    //        {
                    //            long coTEarn = Convert.ToInt64(item["salesAcc"]);
                    //            double coPct = Convert.ToDouble(item["salesShare"]);
                    //            long coTAud = Convert.ToInt64(item["audiAcc"]);

                    //            Movies.Add(new MovieModel()
                    //            {
                    //                Num = (int)item["rank"],
                    //                Name = (string)item["movieNm"],
                    //                ODate = (string)item["openDt"],
                    //                TEarn = coTEarn,
                    //                Pct = coPct,
                    //                TAud = coTAud
                    //            });
                    //        }
                    //    }
                    //}
                    #endregion
                }

                #region 해당 날짜 영화정보 추가 (// 수정전)
                //WebRequest request = WebRequest.Create(RequestURL(Date));
                //request.Method = "GET";
                //request.ContentType = "application/json";

                //using (WebResponse response = request.GetResponse())
                //using (Stream stream = response.GetResponseStream())
                //using (StreamReader reader = new StreamReader(stream))
                //{
                //    string data = reader.ReadToEnd();
                //    var obj = JObject.Parse(data);
                //    var boxOfficeResult = obj["boxOfficeResult"];
                //    var dailyBoxOfficeList = boxOfficeResult["dailyBoxOfficeList"];

                //    foreach (var item in dailyBoxOfficeList)
                //    {
                //        long coTEarn = Convert.ToInt64(item["salesAcc"]);
                //        double coPct = Convert.ToDouble(item["salesShare"]);
                //        long coTAud = Convert.ToInt64(item["audiAcc"]);

                //        Movies.Add(new MovieModel() {
                //            Num = (int)item["rank"],
                //            Name = (string)item["movieNm"],
                //            ODate = (string)item["openDt"],
                //            TEarn = coTEarn,
                //            Pct = coPct,
                //            TAud = coTAud
                //        });
                //    }
                //}
                #endregion
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private ICommand preDateCommand;
        public ICommand PreDateCommand
        {
            get { return (this.preDateCommand ?? (this.preDateCommand = new DelegateCommand(PreDate))); }
        }
        
        private void PreDate()
        {
            if (Movies.Count != 0)
            {
                Movies.Clear();
            }
            DateTime tempDate = DateTime.ParseExact(Date, "yyyyMMdd", null);
            Date = tempDate.AddDays(-1).ToString("yyyyMMdd");

            CuDate = ChangeDate(Date);

            AddMovie();
        }

        private ICommand nextDateCommand;
        public ICommand NextDateCommand
        {
            get { return (this.nextDateCommand ?? (this.nextDateCommand = new DelegateCommand(NextDate))); }
        }

        private void NextDate()
        {
            if (Movies.Count != 0)
            {
                Movies.Clear();
            }
            DateTime tempDate = DateTime.ParseExact(Date, "yyyyMMdd", null);
            Date = tempDate.AddDays(+1).ToString("yyyyMMdd");

            CuDate = ChangeDate(Date);

            AddMovie();
        }

        private string ChangeDate(string Date)
        {
            string result = DateTime.ParseExact(Date, "yyyyMMdd", null).ToString("yyyy년 MM월 dd일 박스오피스");
            return result;
        }
    }   
}
