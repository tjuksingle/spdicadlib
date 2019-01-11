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
        //获取要填写的字符串
        String dlinetext_str = "默认字符串";
        double dlinetext_str_height = 125;
        [CommandMethod("dlinetext")]
        public void dlinetext()
        {
            bool isPointSelected = false;
            while (!isPointSelected)
            {
                //获取要放的位置
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


        [CommandMethod("dcirrr100")]
        public void dcirr100()
        {
            //获取要放的位置
            PromptPointOptions ppo = new PromptPointOptions("点击基点的位置：");
            PromptPointResult ppr = ed.GetPoint(ppo);
            Point3d p3d = new Point3d(0,0,0);
            if (ppr.Status != PromptStatus.OK)
            {
                ed.WriteMessage("出现错误");
                return;
            }
            else
            {
                p3d = ppr.Value;
            }
            Circle cir = new Circle(p3d, Vector3d.ZAxis, 100);
            ToModelSpace(cir);

        }

        //new Func add here!!!
    }
}
