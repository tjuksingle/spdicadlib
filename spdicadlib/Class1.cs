using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Internal;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.LayerManager;
using Autodesk.AutoCAD.Colors;


namespace spdicadlib
{
    public class SpdiLib
    { 
        Editor ed = Application.DocumentManager.MdiActiveDocument.Editor;
        Document doc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;

        //获取要填写的字符串
        String dlinetext_str = "默认字符串";
        double dlinetext_str_height = 125;
        [CommandMethod("dlinetext")]
        public void dlinetext()
        {
            bool isPointSelected = false;
            while (!isPointSelected)
            {
                //获取要放的位置或者修改字符大小
                PromptPointOptions ppo = new PromptPointOptions("点击基点的位置 或者 ");
                ppo.Keywords.Add("S", "S", "修改字符大小(S)");
                PromptPointResult ppr = ed.GetPoint(ppo);

                Point3d p3d = new Point3d(0, 0, 0);
                if (ppr.Status == PromptStatus.Keyword)
                {
                    if (ppr.StringResult == "S")
                    {
                        PromptDoubleOptions pdo = new PromptDoubleOptions("修改字符大小为：");
                        PromptDoubleResult pdr = ed.GetDouble(pdo);
                        dlinetext_str_height = pdr.Value;
                    }
                }
                else if (ppr.Status != PromptStatus.OK)
                {
                    ed.WriteMessage("出现错误");
                    isPointSelected = true;
                    return;
                }
                else
                {
                    p3d = ppr.Value;
                    drawlinetext(p3d);
                    isPointSelected = true;
                }
            }
        }

        //<summary>
        //添加linetext到指定基点
        //</summary>
        //<parpam name = "p3d">linetext的基点（字的左下角）</parpam>
        void drawlinetext(Point3d p3d)
        {
            //获取要填写的字符串
            PromptStringOptions pso = new PromptStringOptions("输入要填写的字符串：");
            PromptResult pr = ed.GetString(pso);
            if (pr.Status != PromptStatus.OK)
            {
                ed.WriteMessage("出现错误");
                return;
            }
            else
            {
                dlinetext_str = pr.StringResult;
            }

            //ToModelSpace(DBtext(p3d, dlinetext_str, dlinetext_str_height));
            //double gain = 1 + Math.Pow(0.8, 0.12 * dlinetext_str.Length + dlinetext_str_height / 75);
            MText mMText = newMtext(new Point3d(p3d.X, p3d.Y + 1.2 * dlinetext_str_height, p3d.Z),
                                  dlinetext_str, dlinetext_str_height, 0, 0, false);
            ToModelSpace(mMText); 
            Extents3d e3d = mMText.GeometricExtents;
            ToModelSpace(new Line(new Point3d(p3d.X, p3d.Y - dlinetext_str_height * 0.25, p3d.Z),
                            new Point3d(p3d.X + e3d.MaxPoint.X - e3d.MinPoint.X + 10 * dlinetext_str.Length, p3d.Y - dlinetext_str_height * 0.25, p3d.Z)));    
        }

        //<summary>
        //添加对象到模型空间
        //</summary>
        //<parpam name = "ent">要添加对对象</parpam>
        //<return>对象ObjectId</return>
        ObjectId ToModelSpace(Entity ent)
        {
            Database db = HostApplicationServices.WorkingDatabase;
            ObjectId entId;
            using (Transaction trans = db.TransactionManager.StartTransaction())
            {
                BlockTable bt = (BlockTable)trans.GetObject(db.BlockTableId, OpenMode.ForRead);
                BlockTableRecord modelSpace = (BlockTableRecord)trans.GetObject(bt[BlockTableRecord.ModelSpace],OpenMode.ForWrite);
                entId = modelSpace.AppendEntity(ent);
                trans.AddNewlyCreatedDBObject(ent, true);
                trans.Commit();
                trans.Dispose();
            }
            return entId;
        }

        //<summary>
        //由插入点、文字内容、文字样式、文字高度创建单行文字
        //</summary>
        //<parpam name = "position">基点</parpam>
        //<parpam name = "textString">文字内容</parpam>
        //<parpam name = "height">文字高度</parpam>
        //<return>对象ObjectId</return>
        DBText Dtext(Point3d position,String textString,double height)
        {
            DBText ent = new DBText();
            ent.Position = position;
            ent.TextString = textString;
            ent.Height = height;
            return ent;
        }

        //<summary>
        //由插入点、文字内容、文字样式、文字高度、文字宽度创建多行文字
        //</summary>
        //<parpam name = "position">基点</parpam>
        //<parpam name = "textString">文字内容</parpam>
        //<parpam name = "height">文字高度</parpam>
        //<parpam name = "width">文字宽度</parpam>
        //<parpam name = "rot">文字转角</parpam>
        //<parpam name = "rot">是否包含域</parpam>
        //<return>多行文字MText</return>
        MText newMtext(Point3d position, String textString, double height,double width,double rot,bool isField)
        {
            MText ent = new MText();
            ent.Location = position;
            ent.TextHeight = height;
            ent.Width = width;
            ent.Rotation = rot;
            if (isField)
            {
                Field field = new Field(textString);
                ent.SetField(field);
            }else
            {
                ent.Contents = textString;
            }
            return ent;
        }

        double dcir_r = 100;
        [CommandMethod("dcir")]
        public void dcir()
        {
            bool isPointSelected = false;
            while(!isPointSelected){
                //获取要放的位置
                PromptPointOptions ppo = new PromptPointOptions("点击圆心的位置 或者 ");
                ppo.Keywords.Add("R", "R", "修改圆的半径(R)");
                PromptPointResult ppr = ed.GetPoint(ppo);

                Point3d p3d = new Point3d(0,0,0);
                if(ppr.Status == PromptStatus.Keyword){
                    if (ppr.StringResult == "R")
                    {
                        dcir_r = getNewDouble("\n修改半径大小为");
                    }
                }else if(ppr.Status != PromptStatus.OK){
                    ed.WriteMessage("出现错误");
                }else{
                    p3d = ppr.Value;
                    Circle cir = new Circle(p3d, Vector3d.ZAxis, dcir_r);
                    ToModelSpace(cir);
                    isPointSelected = true;
                }
            }
        }

        double dtable_gradient = 100; 
        Point3d arg = new Point3d(0,-1,0);
        [CommandMethod("dtable")]
        public void dtable()
        {
            bool isEnd = false;
            uint count = 0;
            uint times = 1;
            //获取要放的位置
            PromptPointOptions ppo;
            ppo = new PromptPointOptions("\n点击确定点的位置 或者 ");
            ppo.Keywords.Add("H", "H", "输入长度(H)");
            ppo.Keywords.Add("L", "L", "获取长度(L)");
            ppo.Keywords.Add("T", "T", "倍数(T)");
            ppo.Keywords.Add("D", "D", "修改方向(D)");
            Point3d p3dBefore = new Point3d(0, 0, 0);
            while (!isEnd)
            {
                PromptPointResult ppr = ed.GetPoint(ppo);

                Point3d p3dAfter = new Point3d(0, 0, 0);
                if (ppr.Status == PromptStatus.Keyword)
                {
                    if (ppr.StringResult == "H")
                    {
                        dtable_gradient = getNewDouble("\n输入长度");
                    } 
                    if (ppr.StringResult == "L")
                    {
                        //PromptDistanceOptions pdo = new PromptDistanceOptions("点击两点确定距离");
                        PromptDoubleResult pdr = ed.GetDistance("点击两点确定距离");
                        dtable_gradient = pdr.Value;
                    }
                    if (ppr.StringResult == "T")
                    {
                        //PromptDistanceOptions pdo = new PromptDistanceOptions("点击两点确定距离");
                        PromptIntegerOptions pio = new PromptIntegerOptions("确定倍数");
                        PromptIntegerResult pir = ed.GetInteger(pio);
                        if (pir.Value <= 0)
                        {
                            ed.WriteMessage("非法输入！");
                        }
                        else
                        {
                            times = (uint)(pir.Value);
                        }
                    } 
                    if (ppr.StringResult == "D")
                    {
                        PromptKeywordOptions pko = new PromptKeywordOptions("选择属性：");
                        pko.Keywords.Add("XP", "XP", "X正半轴(XP)");
                        pko.Keywords.Add("XN", "XN", "X负半轴(XN)");
                        pko.Keywords.Add("YP", "YP", "Y正半轴(YP)");
                        pko.Keywords.Add("YN", "YN", "Y负半轴(YN)");
                        pko.Keywords.Add("ZP", "ZP", "Z正半轴(ZP)");
                        pko.Keywords.Add("ZN", "ZN", "Z负半轴(ZN)");

                        PromptResult pr = ed.GetKeywords(pko);
                        if (pr.Status == PromptStatus.OK)
                        {
                            if (pr.StringResult == "XP")
                            {
                                arg = new Point3d(1, 0, 0);
                            }
                            else if (pr.StringResult == "XN")
                            {
                                arg = new Point3d(-1, 0, 0);
                            }
                            else if (pr.StringResult == "YP")
                            {
                                arg = new Point3d(0, 1, 0);
                            }
                            else if (pr.StringResult == "YN")
                            {
                                arg = new Point3d(0, -1, 0);
                            }
                            else if (pr.StringResult == "ZP")
                            {
                                arg = new Point3d(0, 0, 1);
                            }
                            else if (pr.StringResult == "ZN")
                            {
                                arg = new Point3d(0, 0, -1);
                            }
                        }
                    }
                }
                else if (ppr.Status != PromptStatus.OK)
                {
                    ed.WriteMessage("结束了");
                    isEnd = true;
                }
                else
                {
                    p3dAfter = ppr.Value;
                    if (count == 0) {
                        p3dBefore = ppr.Value;
                    }
                    else 
                    {
                        dtableLine(p3dBefore, p3dAfter, dtable_gradient, arg, times);
                        p3dBefore = p3dAfter;
                    }
                    count++;
                }
            }
        }

        double dtb_gradient = 100;
        [CommandMethod("dtb")]
        public void dtb()
        {
            bool isEnd = false;
            uint count = 0;
            uint times = 1;
            Point3d bePoint = Point3d.Origin;
            Point3d endPoint = Point3d.Origin;
            //获取已有距离
            double be = getdi("\n选择已有长度获取点");
            //获取要放的位置
            PromptPointOptions ppo;
            ppo = new PromptPointOptions("\n点击确定点的位置(第一个输入的点将为起点) 或者 ");
            ppo.Keywords.Add("H", "H", "输入长度(H)");
            ppo.Keywords.Add("L", "L", "获取长度(L)");
            ppo.Keywords.Add("T", "T", "倍数(T)");
            ppo.Keywords.Add("D", "D", "修改方向(D)");
            ppo.Keywords.Add("E", "E", "结束(E)");
            Point3d p3dBefore = new Point3d(0, 0, 0);
            while (!isEnd)
            {
                PromptPointResult ppr = ed.GetPoint(ppo);

                Point3d p3dAfter = new Point3d(0, 0, 0);
                if (ppr.Status == PromptStatus.Keyword)
                {
                    if (ppr.StringResult == "H")
                    {
                        dtable_gradient = getNewDouble("\n输入长度");
                    }
                    if (ppr.StringResult == "L")
                    {
                        //PromptDistanceOptions pdo = new PromptDistanceOptions("点击两点确定距离");
                        PromptDoubleResult pdr = ed.GetDistance("点击两点确定距离");
                        dtable_gradient = pdr.Value;
                    }
                    if (ppr.StringResult == "T")
                    {
                        //PromptDistanceOptions pdo = new PromptDistanceOptions("点击两点确定距离");
                        PromptIntegerOptions pio = new PromptIntegerOptions("确定倍数");
                        PromptIntegerResult pir = ed.GetInteger(pio);
                        if (pir.Value <= 0)
                        {
                            ed.WriteMessage("非法输入！");
                        }
                        else
                        {
                            times = (uint)(pir.Value);
                        }
                    }
                    if (ppr.StringResult == "D")
                    {
                        PromptKeywordOptions pko = new PromptKeywordOptions("选择属性：");
                        pko.Keywords.Add("XP", "XP", "X正半轴(XP)");
                        pko.Keywords.Add("XN", "XN", "X负半轴(XN)");
                        pko.Keywords.Add("YP", "YP", "Y正半轴(YP)");
                        pko.Keywords.Add("YN", "YN", "Y负半轴(YN)");
                        pko.Keywords.Add("ZP", "ZP", "Z正半轴(ZP)");
                        pko.Keywords.Add("ZN", "ZN", "Z负半轴(ZN)");

                        PromptResult pr = ed.GetKeywords(pko);
                        if (pr.Status == PromptStatus.OK)
                        {
                            if (pr.StringResult == "XP")
                            {
                                arg = new Point3d(1, 0, 0);
                            }
                            else if (pr.StringResult == "XN")
                            {
                                arg = new Point3d(-1, 0, 0);
                            }
                            else if (pr.StringResult == "YP")
                            {
                                arg = new Point3d(0, 1, 0);
                            }
                            else if (pr.StringResult == "YN")
                            {
                                arg = new Point3d(0, -1, 0);
                            }
                            else if (pr.StringResult == "ZP")
                            {
                                arg = new Point3d(0, 0, 1);
                            }
                            else if (pr.StringResult == "ZN")
                            {
                                arg = new Point3d(0, 0, -1);
                            }
                        }
                    }
                    if (ppr.StringResult == "E")
                    {
                        endPoint = new Point3d(p3dAfter.X + arg.X * (dtable_gradient + be), p3dAfter.Y + arg.Y * (dtable_gradient + be), p3dAfter.Z + arg.Z * (dtable_gradient + be));
                        Line line = new Line(bePoint, endPoint);
                        ToModelSpace(line);
                    }
                }
                else if (ppr.Status != PromptStatus.OK)
                {
                    ed.WriteMessage("结束了");
                    isEnd = true;
                }
                else
                {
                    p3dAfter = ppr.Value;
                    if (count == 0)
                    {
                        Point3d tempPoint = ppr.Value;
                        p3dBefore = tempPoint;
                        bePoint = new Point3d(tempPoint.X + arg.X * (dtable_gradient + be), tempPoint.Y + arg.Y * (dtable_gradient + be), tempPoint.Z + arg.Z * (dtable_gradient + be));
                    }
                    else
                    {
                        dtableLine(p3dBefore, p3dAfter, dtable_gradient + be, arg, times);
                        p3dBefore = p3dAfter;
                    }
                    count++;
                }
            }
        }


        //<summary>
        //由起始点、终点信息、表格高度和方向标识Point3d生成表格所需要的直线
        //</summary>
        //<parpam name = "p3db">起始点的Point3d</parpam>
        //<parpam name = "p3de">起始点的Point3d</parpam>
        //<parpam name = "gradient">表格高度</parpam>
        //<parpam name = "arg">方向标识单位矢量</parpam>
        //<parpam name = "times">倍数</parpam>
        //<return>对象ObjectId</return>
        public void dtableLine(Point3d p3db, Point3d p3de,double gradient, Point3d arg, uint times)
        {
            for (int i = 1; i <= times; i++)
            {
                Line line = new Line(new Point3d(p3db.X + arg.X * gradient * i, p3db.Y + arg.Y * gradient * i, p3db.Z + arg.Z * gradient * i),
                                    new Point3d(p3de.X + arg.X * gradient * i, p3de.Y + arg.Y * gradient * i, p3de.Z + arg.Z * gradient * i));
                ToModelSpace(line);
            }
            Line line1 = new Line(p3db,
                                new Point3d(p3db.X + arg.X * gradient * times, p3db.Y + arg.Y * gradient * times, p3db.Z + arg.Z * gradient * times));
            Line line2 = new Line(p3de,
                                new Point3d(p3de.X + arg.X * gradient * times, p3de.Y + arg.Y * gradient * times, p3de.Z + arg.Z * gradient * times));
            ToModelSpace(line1);
            ToModelSpace(line2);
        }

        double scaleFactor = 2;
        [CommandMethod("dsc")]
        public void dsc()
        {
            //添加实体对象
            DBObjectCollection dboc = collection();
            if (dboc == null) { ed.WriteMessage("任务中止"); return; }

            //获取基点
            PromptKeywordOptions pko = new PromptKeywordOptions("缩放比例指定方式：");
            pko.Keywords.Add("C", "C", "选择缩放比例参照物(C)");
            pko.Keywords.Add("F", "F", "指定缩放比例（F）");
            PromptResult pr = ed.GetKeywords(pko);

            if (pr.Status == PromptStatus.OK)
            {
                if(pr.StringResult == "C")
                {
                    //计算缩放比例
                    double factor1;
                    double factor2;
                    PromptDoubleResult pdr1 = ed.GetDistance("\n确定原始图形尺寸参照物");
                    factor1 = pdr1.Value;
                    PromptDoubleResult pdr2 = ed.GetDistance("\n确定缩放图形尺寸参照物");
                    factor2 = pdr2.Value;
                    scaleFactor = factor2 / factor1;
                }
                if(pr.StringResult == "F")
                {
                    scaleFactor = getNewDouble("\n指定缩放比例");
                    if (scaleFactor == 0){ed.WriteMessage("任务中止"); return;}
                }
            }

            PromptPointOptions ppo = new PromptPointOptions("\n获取基点");
            PromptPointResult ppr = ed.GetPoint(ppo);
            Point3d basePt;
            if (ppr.Status == PromptStatus.OK)
            {
                basePt = ppr.Value;
            }
            else{ ed.WriteMessage("出现错误，任务中止");return; }

            ppo = new PromptPointOptions("\n获取目标点");
            ppr = ed.GetPoint(ppo);
            Point3d targetPt;
            if (ppr.Status == PromptStatus.OK)
            {
                targetPt = ppr.Value;
            }
            else{ ed.WriteMessage("出现错误，任务中止");return; }
            
            Database db = doc.Database;
            using (Transaction trans = db.TransactionManager.StartTransaction())
            {
                foreach (DBObject obj in dboc)
                {
                    Entity ent = obj as Entity;
                    if (ent != null)
                    {
                        Entity entn;
                        entn = (Entity)trans.GetObject(ent.ObjectId, OpenMode.ForWrite, true);
                        //移动实体
                        move(entn, basePt, targetPt);
                        //缩放实体
                        scale(entn,targetPt, scaleFactor);
                    }
                }

                trans.Commit();
                trans.Dispose();
            }
        }

        //<summary>
        //获取实体集合
        //</summary>
        //<return>实体集合DBObjectCollection</return>
        public DBObjectCollection collection()
        {
            Entity ent = null;
            DBObjectCollection entCollection = new DBObjectCollection();
            PromptSelectionResult psr = ed.GetSelection();
            if (psr.Status == PromptStatus.OK)
            {
                Database db = doc.Database;
                using (Transaction trans = db.TransactionManager.StartTransaction())
                {
                    SelectionSet ss = psr.Value;
                    foreach (ObjectId id in ss.GetObjectIds())
                    {
                        ent = (Entity)trans.GetObject(id, OpenMode.ForWrite, true);
                        if(ent != null) entCollection.Add(ent);
                    }
                    trans.Commit();
                    trans.Dispose();
                }
            }
            else
            {
                return null;
            }
            return entCollection;
        }

        //<summary>
        //移动单个实体
        //</summary>
        //<parpam name = "ent">需要移动的实体</parpam>
        //<parpam name = "basePt">移动点的基点</parpam>
        //<parpam name = "targetPt">一动点的目标点</parpam>
        //<return>无返回类型</return>
        public void move(Entity ent, Point3d basePt, Point3d targetPt)
        {
            Vector3d vec = targetPt - basePt;
            Matrix3d mt = Matrix3d.Displacement(vec);
            ent.TransformBy(mt);
        }


        //<summary>
        //指定基点与比例缩放实体
        //</summary>
        //<parpam name = "ent">需要缩放的实体</parpam>
        //<parpam name = "basePt">基点</parpam>
        //<parpam name = "scaleFactor">缩放比例</parpam>
        //<return>对象ObjectId</return>
        public void scale(Entity ent, Point3d basePt, double scaleFactor)
        {
            Matrix3d mt = Matrix3d.Scaling(scaleFactor,basePt);
            ent.TransformBy(mt);
        }

        double cirr = 480;
        double arcr = 340;
        double sAngel = 180;
        double nAngel = 90;
        double textHeight = 375;
        double daa_consult = 1;
        [CommandMethod("daa")]
        public void daa()
        {
            PromptPointOptions ppo = new PromptPointOptions("\n选择基点 或者 ");
            ppo.Keywords.Add("C","C","获取参照");
            ppo.Keywords.Add("CR", "CR", "修改外圆半径（CR）");
            ppo.Keywords.Add("AR", "AR", "修改内弧半径（AR）");
            ppo.Keywords.Add("N", "N", "修改指北角度（N）");
            ppo.Keywords.Add("S", "S", "修改字体大小");
            
            Point3d basePt = new Point3d(0,0,0);
            bool isEnd = false;
            while (!isEnd)
            {
                PromptPointResult ppr = ed.GetPoint(ppo);
                if (ppr.Status == PromptStatus.OK)
                {
                    basePt = ppr.Value;
                    PromptDoubleOptions pdo = new PromptDoubleOptions("\n方向角大小（需为与正半轴的夹角）");
                    PromptDoubleResult pdr = ed.GetDouble(pdo);
                    if(pdr.Status == PromptStatus.OK)
                    {
                        sAngel = nAngel - pdr.Value;
                        ToModelSpace(new Circle(basePt, Vector3d.ZAxis, cirr*daa_consult));
                        ToModelSpace(new Arc(basePt, Vector3d.ZAxis,arcr * daa_consult, angelToRad(sAngel), angelToRad(nAngel)));
                        ToModelSpace(new Line(basePt, new Point3d(basePt.X + cirr * daa_consult * Math.Cos(angelToRad(nAngel)),
                                                                  basePt.Y + cirr * daa_consult * Math.Sin(angelToRad(nAngel)),
                                                                  basePt.Z)));
                        ToModelSpace(new Line(new Point3d(basePt.X + cirr * daa_consult * Math.Cos(angelToRad(nAngel)),
                                                                  basePt.Y + cirr * daa_consult * Math.Sin(angelToRad(nAngel)),
                                                                  basePt.Z) , 
                                              new Point3d(basePt.X + cirr * daa_consult * Math.Cos(angelToRad(nAngel)) + (cirr - arcr) * daa_consult * Math.Cos(angelToRad(nAngel + 210)),
                                                                  basePt.Y + cirr * daa_consult * Math.Sin(angelToRad(nAngel)) + (cirr - arcr) * daa_consult * Math.Sin(angelToRad(nAngel + 210)),
                                                                  basePt.Z)));
                        ToModelSpace(newMtext(new Point3d(basePt.X + (cirr * daa_consult + textHeight) * Math.Cos(angelToRad(nAngel)),
                                                                  basePt.Y + (cirr * daa_consult + textHeight) * Math.Sin(angelToRad(nAngel)) + 1.2 * textHeight,
                                                                  basePt.Z),
                                                                  "N", textHeight,0,0,false));
                        ToModelSpace(new Line(basePt, new Point3d(basePt.X + cirr * daa_consult * Math.Cos(angelToRad(sAngel)),
                                                                  basePt.Y + cirr * daa_consult * Math.Sin(angelToRad(sAngel)),
                                                                  basePt.Z)));
                        ToModelSpace(new Line(new Point3d(basePt.X + cirr * daa_consult * Math.Cos(angelToRad(sAngel)),
                                                                  basePt.Y + cirr * daa_consult * Math.Sin(angelToRad(sAngel)),
                                                                  basePt.Z), 
                                              new Point3d(basePt.X + cirr * daa_consult * Math.Cos(angelToRad(sAngel)) + (cirr - arcr) * daa_consult * Math.Cos(angelToRad(sAngel + 210)),
                                                                  basePt.Y + cirr * daa_consult * Math.Sin(angelToRad(sAngel)) + (cirr - arcr) * daa_consult * Math.Sin(angelToRad(sAngel + 210)),
                                                                  basePt.Z)));
                        ToModelSpace(newMtext(new Point3d(basePt.X + (cirr * daa_consult + textHeight) * Math.Cos(angelToRad(sAngel)),
                                                                  basePt.Y + (cirr * daa_consult + textHeight) * Math.Sin(angelToRad(sAngel)) + 1.2 * textHeight,
                                                                  basePt.Z), pdr.Value.ToString()+"°", textHeight,0,0,false));
                        isEnd = true;
                    }
                    else
                    {
                        ed.WriteMessage("出现错误，任务终止。");
                        return;
                    }
                }
                if(ppr.Status == PromptStatus.Keyword)
                {
                    ppo.Keywords.Add("C", "C", "获取参照");
                    ppo.Keywords.Add("CR", "CR", "修改外圆半径（CR）");
                    ppo.Keywords.Add("AR", "AR", "修改内弧半径（AR）");
                    ppo.Keywords.Add("N", "N", "修改指北角度（N）");
                    ppo.Keywords.Add("S", "S", "修改字体大小(S)");

                    if (ppr.StringResult == "C")
                    {
                        double consult = getdi("\n选择参照物：");
                        daa_consult = consult / 480;
                    }
                    if (ppr.StringResult == "CR")
                    {
                        cirr = getNewDouble("\n修改外圆半径");
                        if (cirr == 0)
                        {
                            cirr = 480;
                            ed.WriteMessage("填写非法");
                            isEnd = true;
                        }
                    }
                    if(ppr.StringResult == "AR")
                    {
                        arcr = getNewDouble("\n修改外圆半径");
                        if (arcr == 0)
                        {
                            arcr = 340;
                            ed.WriteMessage("填写非法");
                            isEnd = true;
                        }
                    }
                    if(ppr.StringResult == "N")
                    {
                        nAngel = getNewDouble("\n修改指北角度（与x轴正向所成的夹角）");
                    }
                    if(ppr.StringResult == "S")
                    {
                        arcr = getNewDouble("\n修改字体大小");
                        if (arcr == 0)
                        {
                            arcr = 375;
                            ed.WriteMessage("填写非法");
                            isEnd = true;
                        }
                    }
                }
            }
        }

        Double getDi(Point3d p1, Point3d p2)
        {
            return Math.Sqrt(Math.Pow(p1.X - p2.X, 2) + Math.Pow(p1.Y - p2.Y, 2) + Math.Pow(p1.Y - p2.Y, 2));
        }


        double angelToRad(double angel)
        {
            double rad = (Math.PI*angel)/180;
            return rad;
        }
        
        [CommandMethod("dtf")]
        public void dtf()
        {
            int lutDiraction = 1;
            PromptKeywordOptions pko = new PromptKeywordOptions("选择对齐方向");
            pko.Keywords.Add("XL", "XL", "竖直方向左对齐(XL)");
            pko.Keywords.Add("XR", "XR", "竖直方向右对齐(XR)");
            pko.Keywords.Add("XC", "XC", "竖直方向居中对齐(XC)");
            pko.Keywords.Add("Y", "Y", "水平方向对齐(Y)");
            pko.Keywords.Add("Z", "Z", "前后方向对齐(Z)");
            PromptResult pr = ed.GetKeywords(pko);
            if (pr.Status == PromptStatus.OK)
            {
                if (pr.StringResult == "XL")
                {
                    lutDiraction = 0;
                }
                if (pr.StringResult == "XR")
                {
                    lutDiraction = 1;
                }
                if (pr.StringResult == "XC")
                {
                    lutDiraction = 2;
                }
                if (pr.StringResult == "Y")
                {
                    lutDiraction = 3;
                }
                if (pr.StringResult == "Z")
                {
                    lutDiraction = 4;
                }
            }

            bool isEnd = false;
            PromptEntityOptions peo = new PromptEntityOptions("\n选择一个对象");
            PromptEntityResult per = ed.GetEntity(peo);
            if(per.Status == PromptStatus.OK)
            {
                Database db = doc.Database;
                using (Transaction trans = db.TransactionManager.StartTransaction())
                {
                    Entity ent = (Entity)trans.GetObject(per.ObjectId, OpenMode.ForWrite, true);
                    Extents3d e3d = ent.GeometricExtents;
                    Point3d p3dmin = e3d.MinPoint;
                    Point3d p3dmax = e3d.MaxPoint;
                    peo.Keywords.Add("E", "E", "结束(E)");
                    while (!isEnd)
                    {
                        per = ed.GetEntity(peo);
                        if (per.Status == PromptStatus.OK)
                        {
                            Entity entn = (Entity)trans.GetObject(per.ObjectId, OpenMode.ForWrite, true);
                            Extents3d e3dn = entn.GeometricExtents;
                            if (lutDiraction == 0)//XL
                            {
                                move(entn, e3dn.MinPoint, new Point3d(p3dmin.X, e3dn.MinPoint.Y, e3dn.MinPoint.Z));
                            }
                            if (lutDiraction == 1)//XR
                            {
                                move(entn, e3dn.MaxPoint, new Point3d(p3dmax.X, e3dn.MinPoint.Y, e3dn.MinPoint.Z));
                            }
                            if (lutDiraction == 2)//XC
                            {
                                move(entn, new Point3d((e3dn.MinPoint.X+ e3dn.MaxPoint.X)/2, (e3dn.MinPoint.Y + e3dn.MaxPoint.Y) / 2, (e3dn.MinPoint.Z + e3dn.MaxPoint.Z) / 2),
                                           new Point3d((p3dmax.X+p3dmin.X)/2, (e3dn.MinPoint.Y + e3dn.MaxPoint.Y) / 2, (e3dn.MinPoint.Z + e3dn.MaxPoint.Z) / 2));
                            }
                            if (lutDiraction == 3)//YC
                            {
                                move(entn, new Point3d((e3dn.MinPoint.X + e3dn.MaxPoint.X) / 2, (e3dn.MinPoint.Y + e3dn.MaxPoint.Y) / 2, (e3dn.MinPoint.Z + e3dn.MaxPoint.Z) / 2),
                                           new Point3d((e3dn.MinPoint.X + e3dn.MaxPoint.X) / 2, (p3dmax.Y + p3dmin.Y) / 2, (e3dn.MinPoint.Z + e3dn.MaxPoint.Z) / 2));
                            }
                            if (lutDiraction == 4)//ZC
                            {
                                move(entn, new Point3d((e3dn.MinPoint.X + e3dn.MaxPoint.X) / 2, (e3dn.MinPoint.Y + e3dn.MaxPoint.Y) / 2, (e3dn.MinPoint.Z + e3dn.MaxPoint.Z) / 2),
                                           new Point3d((e3dn.MinPoint.X + e3dn.MaxPoint.X) / 2, (e3dn.MinPoint.Y + e3dn.MaxPoint.Y) / 2, (p3dmax.Z + p3dmin.Z) / 2));
                            }
                        }
                        if (per.Status == PromptStatus.Keyword)
                        {
                            if (per.StringResult == "E")
                            {
                                isEnd = true;
                            }
                        }
                        if (per.Status == PromptStatus.Cancel)
                        {
                            isEnd = true;
                        }
                    }
                    trans.Commit();
                    trans.Dispose();
                }
            }
        }

        [CommandMethod("dcenter")]
        public void dcenter()
        {
            PromptEntityOptions peo = new PromptEntityOptions("\n选择一个对象");
            PromptEntityResult per = ed.GetEntity(peo);
            if (per.Status == PromptStatus.OK)
            {
                Database db = doc.Database;
                using (Transaction trans = db.TransactionManager.StartTransaction())
                {
                    Entity ent = (Entity)trans.GetObject(per.ObjectId, OpenMode.ForWrite, true);
                    Extents3d e3d = ent.GeometricExtents;
                    Point3d p3dmin = e3d.MinPoint;
                    Point3d p3dmax = e3d.MaxPoint;

                    PromptPointOptions ppo = new PromptPointOptions("\n获取对角点1");
                    PromptPointResult ppr = ed.GetPoint(ppo);
                    Point3d basePt1;
                    if (ppr.Status == PromptStatus.OK)
                    {
                        basePt1 = ppr.Value;
                    }
                    else { ed.WriteMessage("出现错误，任务中止"); return; }

                    ppo = new PromptPointOptions("\n获取对角点2");
                    ppr = ed.GetPoint(ppo);
                    Point3d basePt2;
                    if (ppr.Status == PromptStatus.OK)
                    {
                        basePt2 = ppr.Value;
                    }
                    else { ed.WriteMessage("出现错误，任务中止"); return; }

                    move(ent, new Point3d((p3dmin.X + p3dmax.X) / 2, (p3dmin.Y + p3dmax.Y) / 2, (p3dmin.Z + p3dmax.Z) / 2),
                              new Point3d((basePt1.X+basePt2.X) / 2,(basePt1.Y + basePt2.Y) / 2,(basePt1.Z + basePt2.Z)/ 2));
                    trans.Commit();
                    trans.Dispose();
                }
            }
            else
            {
                ed.WriteMessage("任务结束。");
            }
        }

        double dnStrHeight = 125;
        [CommandMethod("dn")]
        public void dn()
        {
            bool isEnd = false;
            while (!isEnd)
            {
                PromptPointOptions ppo = new PromptPointOptions("\n选择基点 或者 ");
                ppo.Keywords.Add("S", "S", "修改字体大小(S)");
                PromptPointResult ppr = ed.GetPoint(ppo);
                Point3d basePt;
                if (ppr.Status == PromptStatus.OK)
                {
                    basePt = ppr.Value;
                    ToModelSpace(new Line(basePt,
                                new Point3d(basePt.X + 150, basePt.Y, basePt.Z)));
                    ToModelSpace(new Line(basePt,
                                new Point3d(basePt.X - 150, basePt.Y, basePt.Z)));
                    ToModelSpace(new Line(basePt,
                                new Point3d(basePt.X, basePt.Y - 200, basePt.Z)));
                    ToModelSpace(new Line(basePt,
                                new Point3d(basePt.X, basePt.Y + 800, basePt.Z)));
                    ToModelSpace(new Line(new Point3d(basePt.X, basePt.Y + 800, basePt.Z),
                                new Point3d(basePt.X - 150 * Math.Sin(angelToRad(30)),
                                            basePt.Y + 800 - 150 * Math.Cos(angelToRad(30)),
                                            basePt.Z)));
                    PromptDoubleOptions pdo = new PromptDoubleOptions("\n给定角度（相对于Y轴正向，左负右正）");
                    PromptDoubleResult pdr = ed.GetDouble(pdo);
                    if (pdr.Status == PromptStatus.OK)
                    {
                        double an = pdr.Value;
                        double angel;
                        if (an < 0)
                        {
                            angel = 90 + an;
                        }
                        else
                        {
                            angel = 90 - an;
                        }
                        if (angel == 0 && an != 90 && an != -90)
                        {
                            ToModelSpace(newMtext(new Point3d(basePt.X, basePt.Y + 1000, basePt.Z),
                                             "N", dnStrHeight, 0, 0, false));
                            return;
                        }
                        else
                        {
                            ToModelSpace(new Line(new Point3d((basePt.X + 800 * Math.Cos(angelToRad(angel))) + 150 * Math.Cos(angelToRad(240 - an)),
                                                               (basePt.Y + 800 * Math.Sin(angelToRad(angel))) + 150 * Math.Sin(angelToRad(240 - an)),
                                                               basePt.Z),
                                                  new Point3d(basePt.X + 800 * Math.Cos(angelToRad(angel)), basePt.Y + 800 * Math.Sin(angelToRad(angel)), basePt.Z)));
                            ToModelSpace(new Line(basePt,
                                                  new Point3d(basePt.X + 800 * Math.Cos(angelToRad(angel)), basePt.Y + 800 * Math.Sin(angelToRad(angel)), basePt.Z)));

                            PromptKeywordOptions pko = new PromptKeywordOptions("90°方向是北吗？");
                            pko.Keywords.Add("Y", "Y", "是(Y)");
                            pko.Keywords.Add("N", "N", "否(N)");
                            PromptResult pr = ed.GetKeywords(pko);
                            if (pr.Status == PromptStatus.OK)
                            {
                                if (pr.StringResult == "Y")
                                {

                                    ToModelSpace(newMtext(new Point3d(basePt.X, basePt.Y + 1000, basePt.Z),
                                                      "N", dnStrHeight, 0, 0, false));
                                    ToModelSpace(newMtext(new Point3d(basePt.X + 1000 * Math.Cos(angelToRad(angel)),
                                                                      basePt.Y + 1000 * Math.Sin(angelToRad(angel)),
                                                                      basePt.Z),
                                                          Math.Abs(an).ToString() + "°", dnStrHeight, 0, 0, false));
                                }
                                else
                                {
                                    ToModelSpace(newMtext(new Point3d(basePt.X, basePt.Y + 1000, basePt.Z),
                                                      Math.Abs(an).ToString() + "°", dnStrHeight, 0, 0, false));
                                    ToModelSpace(newMtext(new Point3d(basePt.X + 1000 * Math.Cos(angelToRad(angel)),
                                                                      basePt.Y + 1000 * Math.Sin(angelToRad(angel)),
                                                                      basePt.Z),
                                                          "N", dnStrHeight, 0, 0, false));
                                }
                            }
                        }
                    }
                    isEnd = true;
                }
                else
                {
                    isEnd = true;
                }
                if (ppr.Status == PromptStatus.Keyword)
                {
                    dnStrHeight = getNewDouble("\n输入新的字体大小");
                    if (dnStrHeight == 0)
                    {
                        ed.WriteMessage("\n请输入大于零的数");
                        isEnd = true;
                    }
                }
            }
        }


        Point3d getNewPoint(String message)
        {
            PromptPointOptions pdo = new PromptPointOptions(message);
            PromptPointResult ppr = ed.GetPoint(pdo);
            if (ppr.Status == PromptStatus.OK)
            {
                return ppr.Value;
            }
            else { return Point3d.Origin; }
        }
        Double getNewDouble(String message)
        {
            PromptDoubleOptions pdo = new PromptDoubleOptions(message);
            PromptDoubleResult pdr = ed.GetDouble(pdo);
            if (pdr.Status == PromptStatus.OK)
            {
                return pdr.Value;
            }
            else
            {
                return 0.0;
            }
        }

        Double getdi(String message)
        {
            PromptDoubleResult pdr = ed.GetDistance(message);
            return pdr.Value;
        }


        [CommandMethod("drec")]
        public void drec()
        {
            double w = getNewDouble("\n获取宽度");
            double h = getNewDouble("\n获取高度");
            Point3d pb = getNewPoint("\n获取基点");
            Point3d pe = new Point3d(pb.X + w, pb.Y - h, pb.Z);
            Line newLine = new Line(pb, pe);
            ToModelSpace(newLine);
        }

        double dtvHeight = 2000;
        double dtvTextHeight = 100;
        [CommandMethod("dtv")]
        public void dTableVaule()
        {
            PromptKeywordOptions pko = new PromptKeywordOptions("\n初始化选择");
            pko.Keywords.Add("D", "D", "自动获取距离(D)");
            pko.Keywords.Add("H", "H", "手动输入(H)");
            pko.Keywords.Add("T", "T", "修改字符高度(T)");
            pko.Keywords.Add("H", "H", "手动输入(H)");
            pko.Keywords.Add("S", "S", "保持上次配置(S)");
            PromptResult pr = ed.GetKeywords(pko);
            if (pr.Status == PromptStatus.OK)
            {
                if (pr.StringResult == "D")
                {
                    PromptDoubleResult pdr = ed.GetDistance("点击两点确定距离");
                    dtvHeight = pdr.Value;
                }
                if (pr.StringResult == "H")
                {
                    double temp = getNewDouble("\n输入表格高度");
                    if (temp == 0)
                    {
                        ed.WriteMessage("\n请输入大于零的数");
                    }
                    else
                    {
                        dtvHeight = temp;
                    }
                }
                if (pr.StringResult == "T")
                {
                    double temp = getNewDouble("\n输入字符高度");
                    if (temp == 0)
                    {
                        ed.WriteMessage("\n请输入大于零的数");
                    }
                    else
                    {
                        dtvTextHeight = temp;
                    }
                }
                if (pr.StringResult == "S")
                {
                    
                }
            }

            Point3d p3dB = Point3d.Origin;
            PromptPointOptions ppo = new PromptPointOptions("\n选择一个起点");
            PromptPointResult ppr = ed.GetPoint(ppo);
            if (ppr.Status == PromptStatus.OK)
            {
                p3dB = ppr.Value;
            }
            else
            {
                ed.WriteMessage("出现错误");
            }

            bool isEnd = false;
            int count = 0;
            PromptStringOptions pso = new PromptStringOptions("\n输入一串字符或值");
            while (!isEnd)
            {
                PromptResult psr = ed.GetString(pso);
                if (psr.Status == PromptStatus.OK)
                {
                    ToModelSpace(Dtext(new Point3d(p3dB.X, p3dB.Y - count*dtvHeight, p3dB.Z), psr.StringResult, dtvTextHeight));
                }
                else
                {
                    isEnd = true;
                }
                count++;
            }
        }

        double ddrr = 100;
        //new Func add here!!!
        [CommandMethod("ddr")]
        public void ddr()
        {
            Point3d pc = getNewPoint("\n输入圆心");
            Point3d pd = getNewPoint("\n确定方向");
            PromptKeywordOptions pko = new PromptKeywordOptions("\n选择");
            pko.Keywords.Add("D", "D", "自动获取半径(D)");
            pko.Keywords.Add("H", "H", "手动填入半径(H)");
            pko.Keywords.Add("S", "S", "保持上次配置(S)");
            PromptResult pr = ed.GetKeywords(pko);
            if (pr.Status == PromptStatus.OK)
            {
                if (pr.StringResult == "D")
                {
                    ddrr = getdi("\n选择两点获取长度:");
                }
                if (pr.StringResult == "H")
                {
                    ddrr = getNewDouble("\n请输入长度:");
                }
                if (pr.StringResult == "S")
                {

                }
            }

            int bb = 0;
            int be = 0;
            if (pc.X - pd.X > 0)
            {
                if (pc.Y - pd.Y > 0)
                {
                    bb = 180;
                    be = 270;
                }
                else
                {
                    bb = 90;
                    be = 180;
                }
            }
            else
            {
                if (pc.Y - pd.Y > 0)
                {
                    bb = 270;
                    be = 0;
                }
                else
                {
                    bb = 0;
                    be = 90;
                }
            }


            Arc newArc = new Arc(pc, ddrr, angelToRad(bb), angelToRad(be));
            ToModelSpace(newArc);
        }

        double dhcr = 0;
        [CommandMethod("dhc")]
        public void dhc()
        {
            Point3d pb = getNewPoint("\n确定起点");
            Point3d pd = getNewPoint("\n确定方向");
            PromptKeywordOptions pko = new PromptKeywordOptions("\n选择");
            pko.Keywords.Add("D", "D", "自动获取直径(D)");
            pko.Keywords.Add("H", "H", "手动填入直径(H)");
            pko.Keywords.Add("S", "S", "保持上次配置(S)");

            PromptResult pr = ed.GetKeywords(pko);
            if (pr.Status == PromptStatus.OK)
            {
                if (pr.StringResult == "D")
                {
                    dhcr = getdi("\n选择两点获取长度:")/2;
                }
                if (pr.StringResult == "H")
                {
                    dhcr = getNewDouble("\n请输入长度:")/2;
                }
                if (pr.StringResult == "S")
                {

                }
            }

            Point3d pc = Point3d.Origin;
            int bb = 0;
            int be = 0;
            if (pb.X - pd.X > 0)
            {
                bb = 90;
                be = 270;
                if (pb.Y - pd.Y > 0)
                {
                    pc = new Point3d(pb.X, pb.Y - dhcr, pb.Z);
                }
                else
                {
                    pc = new Point3d(pb.X, pb.Y + dhcr, pb.Z);
                }
            }
            else
            {
                bb = 270;
                be = 90;
                if (pb.Y - pd.Y > 0)
                {
                    pc = new Point3d(pb.X, pb.Y - dhcr, pb.Z);
                }
                else
                {
                    pc = new Point3d(pb.X, pb.Y + dhcr, pb.Z);
                }
            }

            Arc newArc = new Arc(pc, dhcr, angelToRad(bb), angelToRad(be));
            ToModelSpace(newArc);
        }

        String drt_contact = "";
        [CommandMethod("drt")]
        public void drt()
        {
            Database db = doc.Database;
            using (Transaction trans = db.TransactionManager.StartTransaction())
            {
                try {
                    //添加实体对象
                    PromptEntityOptions peo = new PromptEntityOptions("\n获取一个实体对象，获取其内容");
                    PromptEntityResult per = ed.GetEntity(peo);
                    if (per.Status == PromptStatus.OK)
                    {
                        Entity ent = (Entity)trans.GetObject(per.ObjectId, OpenMode.ForRead, true);
                        //DBText dbtext = (DBText)ent;
                        //ed.WriteMessage(dbtext.TextString);
                        ed.WriteMessage(ent.GetRXClass().Name);
                        switch (ent.GetRXClass().Name)
                        {
                            case "AcDbText":
                                DBText dbtext = (DBText)ent;
                                drt_contact = dbtext.TextString;
                                break;
                            case "AcDbMText":
                                MText mtext = (MText)ent;
                                drt_contact = mtext.Text;
                                break;
                            default:
                                return;
                        }
                    }
                    else { }

                    //ed.WriteMessage(y.ToString());
                    PromptEntityOptions peoA = new PromptEntityOptions("\n获取输出实体对象：");
                    PromptEntityResult perA = ed.GetEntity(peoA);
                    if (perA.Status == PromptStatus.OK)
                    {
                        Entity ent = (Entity)trans.GetObject(perA.ObjectId, OpenMode.ForWrite, true);
                        //DBText dbtext = (DBText)ent;
                        //ed.WriteMessage(dbtext.TextString);
                        ed.WriteMessage(ent.GetRXClass().Name);
                        switch (ent.GetRXClass().Name)
                        {
                            case "AcDbText":
                                DBText dbtext = (DBText)ent;
                                dbtext.TextString = drt_contact;
                                break;
                            case "AcDbMText":
                                MText mtext = (MText)ent;
                                mtext.Contents = drt_contact;
                                break;
                            default:
                                return;
                        }
                    }
                    trans.Commit();
                }
                catch { }
                finally
                {

                    trans.Dispose();
                }
            }
        }

        [CommandMethod("dct")]
        public void dct()
        {
            Database db = doc.Database;
            using (Transaction trans = db.TransactionManager.StartTransaction())
            {
                try
                {
                    //ed.WriteMessage(y.ToString());
                    PromptEntityOptions peoA = new PromptEntityOptions("\n获取输出实体对象：");
                    PromptEntityResult perA = ed.GetEntity(peoA);
                    if (perA.Status == PromptStatus.OK)
                    {
                        Entity ent = (Entity)trans.GetObject(perA.ObjectId, OpenMode.ForWrite, true);
                        //DBText dbtext = (DBText)ent;
                        //ed.WriteMessage(dbtext.TextString);
                        ed.WriteMessage(ent.GetRXClass().Name);
                        switch (ent.GetRXClass().Name)
                        {
                            case "AcDbText":
                                DBText dbtext = (DBText)ent;
                                dbtext.TextString = drt_contact;
                                break;
                            case "AcDbMText":
                                MText mtext = (MText)ent;
                                mtext.Contents = drt_contact;
                                break;
                            default:
                                return;
                        }
                    }
                    trans.Commit();
                }
                catch { }
                finally
                {

                    trans.Dispose();
                }
            }
        }
        [CommandMethod("daad")]
        public void daad()
        {
            bool isEnd = false;
            int count = 0;
            int[] x;
            x = new int[100];
            int y = 0;
            Database db = doc.Database;
            using (Transaction trans = db.TransactionManager.StartTransaction())
            {
                try
                {
                    while (!isEnd)
                    {
                        //添加实体对象
                        PromptEntityOptions peo = new PromptEntityOptions("\n获取一个实体对象，引索号：" + count.ToString());
                        PromptEntityResult per = ed.GetEntity(peo);
                        if (per.Status == PromptStatus.OK)
                        {
                            Entity ent = (Entity)trans.GetObject(per.ObjectId, OpenMode.ForRead, true);
                            //DBText dbtext = (DBText)ent;
                            //ed.WriteMessage(dbtext.TextString);
                            //ed.WriteMessage(ent.GetRXClass().Name);
                            switch (ent.GetRXClass().Name)
                            {
                                case "AcDbText":
                                    DBText dbtext = (DBText)ent;
                                    x[count] = int.Parse(dbtext.TextString);
                                    count++;
                                    break;
                                default:
                                    isEnd = true;
                                    break;
                            }
                        }
                        else
                        {
                            isEnd = true;
                        }
                    }
                    for (int i = 0; i < count; i++)
                    {
                        y = y + x[i];
                    }
                    //ed.WriteMessage(y.ToString());
                    PromptEntityOptions peoA = new PromptEntityOptions("\n获取输出实体对象：");
                    PromptEntityResult perA = ed.GetEntity(peoA);
                    if (perA.Status == PromptStatus.OK)
                    {
                        Entity ent = (Entity)trans.GetObject(perA.ObjectId, OpenMode.ForWrite, true);
                        DBText dbt = (DBText)ent;
                        dbt.TextString = y.ToString();

                    }
                    trans.Commit();
                }
                catch{}
                finally
                {
                    trans.Dispose();
                }
                
            }
        }


        [CommandMethod("dcf")]
        public void dcf()
        {
            bool isEnd = false;
            int count = 0;
            int[] x;
            x = new int[10];
            int y = 1;
            Database db = doc.Database;
            using (Transaction trans = db.TransactionManager.StartTransaction())
            {
                try
                {
                    while (!isEnd)
                    {
                        //添加实体对象
                        PromptEntityOptions peo = new PromptEntityOptions("\n获取一个实体对象，引索号：" + count.ToString());
                        PromptEntityResult per = ed.GetEntity(peo);
                        if (per.Status == PromptStatus.OK)
                        {
                            Entity ent = (Entity)trans.GetObject(per.ObjectId, OpenMode.ForRead, true);
                            //DBText dbtext = (DBText)ent;
                            //ed.WriteMessage(dbtext.TextString);
                            //ed.WriteMessage(ent.GetRXClass().Name);
                            switch (ent.GetRXClass().Name)
                            {
                                case "AcDbText":
                                    DBText dbtext = (DBText)ent;
                                    x[count] = int.Parse(dbtext.TextString);
                                    count++;
                                    break;
                                default:
                                    isEnd = true;
                                    break;
                            }
                        }
                        else
                        {
                            isEnd = true;
                        }
                    }
                    for (int i = 0; i < count; i++)
                    {
                        y = y * x[i];
                    }
                    //ed.WriteMessage(y.ToString());
                    PromptEntityOptions peoA = new PromptEntityOptions("\n获取输出实体对象：");
                    PromptEntityResult perA = ed.GetEntity(peoA);
                    if (perA.Status == PromptStatus.OK)
                    {
                        Entity ent = (Entity)trans.GetObject(perA.ObjectId, OpenMode.ForWrite, true);
                        DBText dbt = (DBText)ent;
                        dbt.TextString = y.ToString();

                    }
                    trans.Commit();
                }
                catch { }
                finally
                {
                    trans.Dispose();
                }
                
            }
        }
        
        [CommandMethod("djf")]
        public void djf()
        {
            Database db = doc.Database;
            using (Transaction trans = db.TransactionManager.StartTransaction())
            {
                int x = 0;
                try
                {
                    //ed.WriteMessage(y.ToString());
                    PromptEntityOptions peoA = new PromptEntityOptions("\n获取输出实体对象：");
                    PromptEntityResult perA = ed.GetEntity(peoA);
                    if (perA.Status == PromptStatus.OK)
                    {
                        Entity ent = (Entity)trans.GetObject(perA.ObjectId, OpenMode.ForWrite, true);
                        //DBText dbtext = (DBText)ent;
                        //ed.WriteMessage(dbtext.TextString);
                        ed.WriteMessage(ent.GetRXClass().Name);
                        PromptIntegerOptions pio = new PromptIntegerOptions("\n输入要加进去的数：");
                        PromptIntegerResult pir;
                        switch (ent.GetRXClass().Name)
                        {
                            case "AcDbText":
                                DBText dbtext = (DBText)ent;
                                x = int.Parse(dbtext.TextString);
                                pir = ed.GetInteger(pio);
                                if (pir.Status == PromptStatus.OK)
                                {
                                    int y = pir.Value + x;
                                    dbtext.TextString = y.ToString();
                                }
                                break;
                            case "AcDbMText":
                                MText mtext = (MText)ent;
                                x = int.Parse(mtext.Text);
                                pir = ed.GetInteger(pio);
                                if (pir.Status == PromptStatus.OK)
                                {
                                    int y = pir.Value + x;
                                    mtext.Contents = y.ToString();
                                }
                                break;
                            default:
                                return;
                        }
                    }
                    trans.Commit();
                }
                catch { }
                finally
                {
                    trans.Dispose();
                }
            }
        }

        [CommandMethod("dxf")]
        public void dxf()
        {
            //添加实体对象
            DBObjectCollection dboc = collection();
            if (dboc == null) { ed.WriteMessage("任务中止"); return; }

            //获取基点
            PromptPointOptions ppo = new PromptPointOptions("\n选取聚合点：");
            PromptPointResult ppr = ed.GetPoint(ppo);
            
            Point3d targetPt;
            if (ppr.Status == PromptStatus.OK)
            {
                targetPt = ppr.Value;
            }
            else { ed.WriteMessage("出现错误，任务中止"); return; }
            
            Database db = doc.Database;
            using (Transaction trans = db.TransactionManager.StartTransaction())
            {
                try
                {
                    foreach (DBObject obj in dboc)
                    {
                        Entity ent = obj as Entity;
                        if (ent != null)
                        {
                            Entity entn;
                            entn = (Entity)trans.GetObject(ent.ObjectId, OpenMode.ForWrite, true);
                            if (entn.GetRXClass().Name == "AcDbLine")
                            {
                                Line l = (Line)entn;
                                Point3d ep = l.EndPoint;
                                Point3d sp = l.StartPoint;
                                if (getDi(ep, targetPt) > getDi(sp, targetPt))
                                {
                                    l.StartPoint = targetPt;
                                }
                                else
                                {
                                    l.EndPoint = targetPt;
                                }
                            }
                        }
                    }
                    trans.Commit();
                }
                catch
                {
                    ed.WriteMessage("Error：Unknown Entity.");
                }
                finally
                {
                    trans.Dispose();
                }
                
            }
        }
    }
}