﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using YT.COM;

using TS = Tekla.Structures;
using TSM = Tekla.Structures.Model;
using TSD = Tekla.Structures.Dialog;
using TSG = Tekla.Structures.Geometry3d;
using TSC = Tekla.Structures.Catalogs;
using TSP = Tekla.Structures.Plugins;
using TST = Tekla.Structures.Datatype;
using System.Collections;

namespace YT.WallVerticalRebar
{
    [TSP.Plugin("YT.WallVerticalRebar")]  // Tekla에서 표시되는 PlugIn 이름
    [TSP.PluginUserInterface("YT.WallVerticalRebar.WallVerticalRebarU")] // Form 결합
    public class WallVerticalRebarM : TSP.PluginBase
    {
        public WallVerticalRebarD D { get; set; }
        public TSM.Model M { get; set; }
        public WallVerticalRebarM(WallVerticalRebarD data)
        {
            M = new TSM.Model();
            D = data;
        }
        public override List<InputDefinition> DefineInput()
        {
            List<InputDefinition> partList = new List<InputDefinition>();

            TSM.UI.Picker pickPart = new TSM.UI.Picker();

            var wall = (TSM.Beam)pickPart.PickObject(TSM.UI.Picker.PickObjectEnum.PICK_ONE_PART);

            var startPoints = new TSM.UI.Picker().PickPoints(TSM.UI.Picker.PickPointEnum.PICK_TWO_POINTS);
            var startPoint1 = (TSG.Point)startPoints[0];
            var startPoint2 = (TSG.Point)startPoints[1];

            var endPoints = new TSM.UI.Picker().PickPoints(TSM.UI.Picker.PickPointEnum.PICK_TWO_POINTS);
            var endPoint1 = (TSG.Point)endPoints[0];
            var endPoint2 = (TSG.Point)endPoints[1];

            InputDefinition input1 = new InputDefinition(wall.Identifier);
            InputDefinition input2 = new InputDefinition(startPoint1);
            InputDefinition input3 = new InputDefinition(startPoint2);
            InputDefinition input4 = new InputDefinition(endPoint1);
            InputDefinition input5 = new InputDefinition(endPoint2);

            partList.Add(input1);
            partList.Add(input2);
            partList.Add(input3);
            partList.Add(input4);
            partList.Add(input5);

            return partList;
        }
        public override bool Run(List<InputDefinition> Input)
        {
            var m = new TSM.Model();
            TSM.WorkPlaneHandler workPlanHandler = m.GetWorkPlaneHandler();
            TSM.TransformationPlane currentPlane = workPlanHandler.GetCurrentTransformationPlane();

            var wall = (TSM.Beam)M.SelectModelObject((TS.Identifier)Input[0].GetInput());
            var startPoint1 = (TSG.Point)(Input[1]).GetInput();
            var startPoint2 = (TSG.Point)(Input[2]).GetInput();
            var endPoint1 = (TSG.Point)(Input[3]).GetInput();
            var endPoint2 = (TSG.Point)(Input[4]).GetInput();


            // 좌표 변경
            var lcs = wall.GetCoordinateSystem();

            // 부재 시작점 좌표
            var s_ucs_op = wall.StartPoint;
            var s_ucs_ax = lcs.AxisX.GetNormal();
            var s_ucs_ay = lcs.AxisX.Cross(lcs.AxisY).GetNormal() * -1;
            var s_ucs_tp = new TSM.TransformationPlane(s_ucs_op, s_ucs_ax, s_ucs_ay);

            // 부재 끝점 좌표
            var e_ucs_op = wall.EndPoint;
            var e_ucs_ax = lcs.AxisX.GetNormal() * -1;
            var e_ucs_ay = lcs.AxisX.Cross(lcs.AxisY).GetNormal();
            var e_ucs_tp = new TSM.TransformationPlane(e_ucs_op, e_ucs_ax, e_ucs_ay);

            var ucs_tp = new TSM.TransformationPlane();

            switch (D.W_Coordination)
            {
                case "StartEnd":
                    ucs_tp = s_ucs_tp;
                    break;
                case "EndStart":
                    ucs_tp = e_ucs_tp;
                    break;
                default:
                    ucs_tp = s_ucs_tp;
                    break;
            }

            m.GetWorkPlaneHandler().SetCurrentTransformationPlane(ucs_tp);

            TSG.Point p1 = ucs_tp.TransformationMatrixToLocal.Transform(currentPlane.TransformationMatrixToGlobal.Transform(startPoint1));

            TSG.Point p2 = ucs_tp.TransformationMatrixToLocal.Transform(currentPlane.TransformationMatrixToGlobal.Transform(startPoint2));

            TSG.Point p3 = ucs_tp.TransformationMatrixToLocal.Transform(currentPlane.TransformationMatrixToGlobal.Transform(endPoint1));

            TSG.Point p4 = ucs_tp.TransformationMatrixToLocal.Transform(currentPlane.TransformationMatrixToGlobal.Transform(endPoint2));

            CreatWallRebar(wall, p1, p2, p3, p4);
            return true;
        }

        private void CreatWallRebar(TSM.Beam beam, TSG.Point point1, TSG.Point point2, TSG.Point point3, TSG.Point point4)
        {
            var m = new TSM.Model();

            #region 범위 설정

            var startLineSegment = new TSG.LineSegment();
            startLineSegment.Point1 = point1;
            startLineSegment.Point2 = point2;

            var startControlLine = new TSM.ControlLine();
            startControlLine.Line = startLineSegment;
            startControlLine.Insert();

            var endLineSegment = new TSG.LineSegment();
            endLineSegment.Point1 = point3;
            endLineSegment.Point2 = point4;

            var endControlLine = new TSM.ControlLine();
            endControlLine.Line = endLineSegment;
            endControlLine.Insert();

            #endregion


            #region 철근 공칭지름 관련 이동

            var rightMoveXS = D.R_MoveXS;
            var rightMoveXE = D.R_MoveXE;
            var rightMoveY = D.R_MoveY;

            var rightKsXS = KS.GetDiameter(Convert.ToDouble(D.R_Size)) / 2;
            var rightKsXE = KS.GetDiameter(Convert.ToDouble(D.R_Size)) / 2;
            var rightKsY = KS.GetDiameter(Convert.ToDouble(D.R_Size)) / 2;

            var leftMoveXS = D.L_MoveXS;
            var leftMoveXE = D.L_MoveXE;
            var leftMoveY = D.L_MoveY;

            var leftKsXS = KS.GetDiameter(Convert.ToDouble(D.L_Size)) / 2;
            var leftKsXE = KS.GetDiameter(Convert.ToDouble(D.L_Size)) / 2;
            var leftKsY = KS.GetDiameter(Convert.ToDouble(D.L_Size)) / 2;

            #endregion


            var maxX = beam.GetSolid().MaximumPoint.X;
            var maxY = beam.GetSolid().MaximumPoint.Y;
            var maxZ = beam.GetSolid().MaximumPoint.Z;

            var minX = beam.GetSolid().MinimumPoint.X;
            var minY = beam.GetSolid().MinimumPoint.Y;
            var minZ = beam.GetSolid().MinimumPoint.Z;

            #region 우측철근

            if (rightMoveXS == 0) rightKsXS = 0;
            if (rightMoveXE == 0) rightKsXE = 0;
            if (rightMoveY == 0) rightKsY = 0;

            var rightLineSegment = new TSG.LineSegment();
            rightLineSegment.Point1 = new TSG.Point(minX + rightMoveXS + rightKsXS, minY + rightMoveY + rightKsY, maxZ);
            rightLineSegment.Point2 = new TSG.Point(maxX - rightMoveXE - rightKsXE, minY + rightMoveY + rightKsY, maxZ);

            var rightControlLine = new TSM.ControlLine();
            rightControlLine.Line = rightLineSegment;
            rightControlLine.Insert();

            var startrightCrossPoint = Util.FindPoint.CrossPoint(new TSG.Line(startLineSegment), new TSG.Line(rightLineSegment));
            var startrigthControlPoint = new TSM.ControlPoint(startrightCrossPoint);
            startrigthControlPoint.Insert();

            var endrightCrossPoint = Util.FindPoint.CrossPoint(new TSG.Line(endLineSegment), new TSG.Line(rightLineSegment));
            var endrightContolPoint = new TSM.ControlPoint(endrightCrossPoint);
            endrightContolPoint.Insert();

            var barR = new Rebar();

            barR.Name = D.R_Name;
            barR.Grade = D.R_Grade;
            barR.Size = D.R_Size;
            barR.Radius = D.R_Radius;
            barR.Class = D.R_Class;

            barR.Prefix = D.R_Prefix;
            barR.StartNumber = D.R_StartNumber;

            if (D.W_Coordination == "StartEnd")
            {
                var shapeR = new TSM.Polygon();
                shapeR.Points.Add(new TSG.Point(startrightCrossPoint.X + rightMoveXS + rightKsXS, startrightCrossPoint.Y, minZ));
                shapeR.Points.Add(new TSG.Point(startrightCrossPoint.X + rightMoveXS + rightKsXS, startrightCrossPoint.Y, maxZ));

                barR.Polygon.Add(shapeR);

                barR.StartPoint = new TSG.Point(startrightCrossPoint.X + rightMoveXS + rightKsXS, startrightCrossPoint.Y, startrightCrossPoint.Z);
                barR.EndPoint = new TSG.Point(endrightCrossPoint.X - rightMoveXE - rightKsXE, endrightCrossPoint.Y, endrightCrossPoint.Z);
            }
            else if (D.W_Coordination == "EndStart")
            {
                var shapeR = new TSM.Polygon();
                shapeR.Points.Add(new TSG.Point(endrightCrossPoint.X + rightMoveXS + rightKsXS, startrightCrossPoint.Y, minZ));
                shapeR.Points.Add(new TSG.Point(endrightCrossPoint.X + rightMoveXS + rightKsXS, startrightCrossPoint.Y, maxZ));

                barR.Polygon.Add(shapeR);

                barR.StartPoint = new TSG.Point(endrightCrossPoint.X + rightMoveXS + rightKsXS, startrightCrossPoint.Y, startrightCrossPoint.Z);
                barR.EndPoint = new TSG.Point(startrightCrossPoint.X - rightMoveXE - rightKsXE, endrightCrossPoint.Y, endrightCrossPoint.Z);
            }

            barR.Father = beam;

            double rightSpacing = D.R_Spacing;

            var lengthR = new TSG.LineSegment(barR.StartPoint, barR.EndPoint).Length();

            var rightSpacings = new Spacings();
            barR.Spacing = rightSpacings.SetSpacing(lengthR, rightSpacing);

            switch (D.R_ExcludeType )
            {
                case "없음":
                    barR.ExcludeType = TSM.BaseRebarGroup.ExcludeTypeEnum.EXCLUDE_TYPE_NONE;
                    break;
                case "첫번째":
                    barR.ExcludeType = TSM.BaseRebarGroup.ExcludeTypeEnum.EXCLUDE_TYPE_FIRST;
                    break;
                case "마지막":
                    barR.ExcludeType = TSM.BaseRebarGroup.ExcludeTypeEnum.EXCLUDE_TYPE_LAST;
                    break;
                case "첫번째 및 마지막":
                    barR.ExcludeType = TSM.BaseRebarGroup.ExcludeTypeEnum.EXCLUDE_TYPE_BOTH;
                    break;
                default:
                    barR.ExcludeType = TSM.BaseRebarGroup.ExcludeTypeEnum.EXCLUDE_TYPE_NONE;
                    break;
            }

            barR.Insert();

            #endregion


            #region 좌측철근

            if (leftMoveXS == 0) leftKsXS = 0;
            if (leftMoveXE == 0) leftKsXE = 0;
            if (leftMoveY == 0) leftKsY = 0;

            var leftLineSegment = new TSG.LineSegment();
            leftLineSegment.Point1 = new TSG.Point(minX + leftMoveXS + leftKsXS, maxY - leftMoveY - leftKsY, maxZ);
            leftLineSegment.Point2 = new TSG.Point(maxX - leftMoveXE - leftKsXE, maxY - leftMoveY - leftKsY, maxZ);

            var leftControlLine = new TSM.ControlLine();
            leftControlLine.Line = leftLineSegment;
            leftControlLine.Insert();

            var startleftCrossPoint = Util.FindPoint.CrossPoint(new TSG.Line(startLineSegment), new TSG.Line(leftLineSegment));
            var startleftControlPoint = new TSM.ControlPoint(startleftCrossPoint);
            startleftControlPoint.Insert();

            var endleftCrossPoint = Util.FindPoint.CrossPoint(new TSG.Line(endLineSegment), new TSG.Line(leftLineSegment));
            var endleftControlPoint = new TSM.ControlPoint(endleftCrossPoint);
            endleftControlPoint.Insert();

            var barL = new Rebar();

            barL.Name = D.L_Name;
            barL.Grade = D.L_Grade;
            barL.Size = D.L_Size;
            barL.Radius = D.L_Radius;
            barL.Class = D.L_Class;

            barL.Prefix = D.L_Prefix;
            barL.StartNumber = D.L_StartNumber;

            if (D.W_Coordination == "StartEnd")
            {
                var shapeL = new TSM.Polygon();

                shapeL.Points.Add(new TSG.Point(startleftCrossPoint.X + leftMoveXS + leftKsXS, startleftCrossPoint.Y, minZ));
                shapeL.Points.Add(new TSG.Point(startleftCrossPoint.X + leftMoveXS + leftKsXS, startleftCrossPoint.Y, maxZ));

                barL.Polygon.Add(shapeL);

                barL.StartPoint = new TSG.Point(startleftCrossPoint.X + leftMoveXS + leftKsXS, startleftCrossPoint.Y, startleftCrossPoint.Z);
                barL.EndPoint = new TSG.Point(endleftCrossPoint.X - leftMoveXE - leftKsXE, endleftCrossPoint.Y, endleftCrossPoint.Z);
            }
            else if (D.W_Coordination == "EndStart")
            {
                var shapeL = new TSM.Polygon();

                shapeL.Points.Add(new TSG.Point(endleftCrossPoint.X + leftMoveXS + leftKsXS, startleftCrossPoint.Y, minZ));
                shapeL.Points.Add(new TSG.Point(endleftCrossPoint.X + leftMoveXS + leftKsXS, startleftCrossPoint.Y, maxZ));

                barL.Polygon.Add(shapeL);

                barL.StartPoint = new TSG.Point(endleftCrossPoint.X + leftMoveXS + leftKsXS, startleftCrossPoint.Y, startleftCrossPoint.Z);
                barL.EndPoint = new TSG.Point(startleftCrossPoint.X - leftMoveXE - leftKsXE, endleftCrossPoint.Y, endleftCrossPoint.Z);
            }

            barL.Father = beam;

            double leftSpacing = D.L_Spacing;

            var lengthL = new TSG.LineSegment(barL.StartPoint, barL.EndPoint).Length();

            var leftSpacings = new Spacings();
            barL.Spacing = leftSpacings.SetSpacing(lengthL, leftSpacing);


            switch (D.L_ExcludeType)
            {
                case "없음":
                    barL.ExcludeType = TSM.BaseRebarGroup.ExcludeTypeEnum.EXCLUDE_TYPE_NONE;
                    break;
                case "첫번째":
                    barL.ExcludeType = TSM.BaseRebarGroup.ExcludeTypeEnum.EXCLUDE_TYPE_FIRST;
                    break;
                case "마지막":
                    barL.ExcludeType = TSM.BaseRebarGroup.ExcludeTypeEnum.EXCLUDE_TYPE_LAST;
                    break;
                case "첫번째 및 마지막":
                    barL.ExcludeType = TSM.BaseRebarGroup.ExcludeTypeEnum.EXCLUDE_TYPE_BOTH;
                    break;
                default:
                    barL.ExcludeType = TSM.BaseRebarGroup.ExcludeTypeEnum.EXCLUDE_TYPE_NONE;
                    break;
            }

            barL.Insert();

            #endregion
        }
    }
}
