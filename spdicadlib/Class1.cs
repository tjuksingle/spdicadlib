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
    enum Dircetion
    {
        HorizonDown = 1,
        HorizonUp = 2,
        VerticalDown = 3,
        VerticalUp = 4,
    }

    public class SpdiLib
    { 
        Editor ed = Application.DocumentManager.MdiActiveDocument.Editor;

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
            
            //Point3dCollection pc = new Point3dCollection(new Point3d[] { new Point3d(20, 10, 0), new Point3d(35, -5, 0), new Point3d(80, 0, 0) });
            //Polyline3d pl = new Polyline3d(Poly3dType.QuadSplinePoly, pc, true);
            //ToModelSpace(pl);
            //Circle cir = new Circle(Point3d.Origin, Vector3d.ZAxis, 15);
            //ToModelSpace(cir);
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

            ToModelSpace(DBtext(p3d, dlinetext_str, dlinetext_str_height));
            double gain = 1+Math.Pow(0.8,0.12*dlinetext_str.Length+dlinetext_str_height/75);
            ToModelSpace(new Line(new Point3d(p3d.X, p3d.Y - dlinetext_str_height * 0.25, p3d.Z),
                            new Point3d(p3d.X + (double)(dlinetext_str_height * gain * dlinetext_str.Length), p3d.Y - dlinetext_str_height * 0.25, p3d.Z)));    
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
        DBText DBtext(Point3d position,String textString,double height)
        {
            DBText ent = new DBText();
            ent.Position = position;
            ent.TextString = textString;
            ent.Height = height;
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
                        PromptDoubleOptions pdo = new PromptDoubleOptions("修改半径大小为：");
                        PromptDoubleResult pdr = ed.GetDouble(pdo);
                        dcir_r = pdr.Value;
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
            //获取要放的位置
            PromptPointOptions ppo;
            ppo = new PromptPointOptions("\n点击确定点的位置 或者 ");
            ppo.Keywords.Add("H", "H", "输入长度(H)");
            ppo.Keywords.Add("L", "L", "获取长度(L)");
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
                        PromptDoubleOptions pdo = new PromptDoubleOptions("输入长度：");
                        PromptDoubleResult pdr = ed.GetDouble(pdo);
                        dtable_gradient = pdr.Value;
                    } 
                    if (ppr.StringResult == "L")
                    {
                        //PromptDistanceOptions pdo = new PromptDistanceOptions("点击两点确定距离");
                        PromptDoubleResult pdr = ed.GetDistance("点击两点确定距离");
                        dtable_gradient = pdr.Value;
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
                        dtableLine(p3dBefore, p3dAfter, dtable_gradient, arg);
                        p3dBefore = p3dAfter;
                    }
                    count++;
                }
            }
        }

        public void dtableLine(Point3d p3db, Point3d p3de,double gradient, Point3d arg)
        {
            Line line = new Line(p3db,
                                                new Point3d(p3db.X + arg.X * gradient, p3db.Y + arg.Y * gradient, p3db.Z + arg.Z * gradient));
            Line line1 = new Line(new Point3d(p3db.X + arg.X * gradient, p3db.Y + arg.Y * gradient, p3db.Z + arg.Z * gradient),
                                new Point3d(p3de.X + arg.X * gradient, p3de.Y + arg.Y * gradient, p3de.Z + arg.Z * gradient));
            Line line2 = new Line(p3de,
                                new Point3d(p3de.X + arg.X * gradient, p3de.Y + arg.Y * gradient, p3de.Z + arg.Z * gradient));
            ToModelSpace(line);
            ToModelSpace(line1);
            ToModelSpace(line2);
        }

        //new Func add here!!!
    }
}
