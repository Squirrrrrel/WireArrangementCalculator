using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace WireArrangementCalculator
{
    public class SlotPreprocessor
    {
        public double Height { get; private set; }
        public double Area { get; private set; }
        protected double _h_1;
        protected double _h_2;
        protected double _b_so;
        protected double _b_1;
        protected double _b_2;
        protected double _R;
        protected double _r;
        public Point BasePoint { get; private set; }
        public double Insulation { get; private set; }
        internal Point[] _outlineBase { get; set; }
        public SlotPreprocessor(double h_slot, double h_1, double h_2,
            double b_so, double b_1, double b_2,
            double R, double r, double d_iso)
        {
            Height = h_slot;
            _h_1 = h_1;
            _h_2 = h_2;
            _b_so = b_so;
            _b_1 = b_1;
            _b_2 = b_2;
            _R = R;
            _r = r;
            Insulation = d_iso;
            _getSlotPoints();
        }
        private void _getSlotPoints()
        {
            double ang1 = Math.Asin(this._b_so / 2.0 / this._R);
            Point p1 = new Point(this._R * Math.Cos(ang1), -this._b_so / 2.0);
            Point p2 = new Point(p1.X + this._h_1, p1.Y);
            Point p3 = new Point(p2.X + this._h_2, -this._b_1 / 2.0);

            var x5 = this._R + this.Height;
            var y4 = -this._b_2 / 2.0;
            var xm = x5 - this._r;

            Func<double, double> ym = x => y4 - (x - p3.X) * (xm - x) / (y4 - p3.Y);
            Func<double, double> equation = x => Math.Pow(xm - x, 2.0) +
            Math.Pow(ym(x) - y4, 2.0) - Math.Pow(this._r, 2.0);
            Func<double, double> diff_equation = x => (equation(x + 1e-9) - equation(x - 1e-9)) * 2e9;

            double x4, y5;
            if (Math.Abs(y4 - p3.Y) > 1e-8)
            {
                var x_now = xm - this._r;
                for (int i = 0; i < 1000000; i++)
                {
                    var x_last = x_now;
                    x_now = x_last - equation(x_last) / diff_equation(x_last);
                    if (Math.Abs(equation(x_now)) < 1e-10)
                    {
                        if ((x_now > xm - this._r) && (x_now < x5) && (ym(x_now) > y4)) break;
                    }
                }
                x4 = x_now;
                y5 = ym(x_now);
            }
            else
            {
                x4 = x5 - this._r;
                y5 = y4 + this._r;
            }
            Point p4 = new Point(x4, y4);
            Point p5 = new Point(x5, y5);
            Point pm = new Point(xm, y5);

            double ang2 = Point.GetAngleRad(pm, p4, p5);
            Point p45_1 = p4.RotateByRad(pm, ang2 * 1.0 / 5.0);
            Point p45_2 = p4.RotateByRad(pm, ang2 * 2.0 / 5.0);
            Point p45_3 = p4.RotateByRad(pm, ang2 * 3.0 / 5.0);
            Point p45_4 = p4.RotateByRad(pm, ang2 * 4.0 / 5.0);

            this._outlineBase = new Point[] {p1, p2, p3, p4, p45_1, p45_2, p45_3, p45_4, p5,
            p5.MirrorX(), p45_4.MirrorX(), p45_3.MirrorX(), p45_2.MirrorX(),
            p45_1.MirrorX(), p4.MirrorX(), p3.MirrorX(), p2.MirrorX(), p1.MirrorX()};

            this.BasePoint = (p5 + p5.MirrorX()) / 2.0;

            var _halfSlot = new List<Point>(new Point[] { p1, p2, p3, p4, p45_1, p45_2, p45_3, p45_4, p5 });
            this.Area = -2.0 * _getAreaByIntegration(_halfSlot, p3.X, p5.X);

        }

        private double _getAreaByIntegration(List<Point> series, double x_start, double x_end)
        {
            double dx = 0.001;
            double ret = 0;
            for (double x = x_start; x < x_end; x += dx)
            {
                double y = _getByIntepolation(series, x);
                ret += y * dx;
            }
            return ret;
        }
        private double _getByIntepolation(List<Point> series, double x)
        {
            double ret = 0.0;
            for (int i = 0; i < series.Count - 1; i++)
            {
                Point p1 = series[i];
                Point p2 = series[i + 1];
                if ((p1.X < x) && (p2.X >= x))
                {
                    ret = (p2.Y - p1.Y) * (x - p1.X) / (p2.X - p1.X) + p1.Y;
                    break;
                }
            }
            return ret;
        }
    }

    public class Slot : SlotPreprocessor, ICloneable
    {
        public Point[] InnerOutline { get; private set; }
        public Point[] Outline { get; private set; }
        public Point[] InnerOutlineRotated { get; private set; }

        internal Point[] _innerOutlineBase { get; set; }
        public double InnerArea { get; private set; }

        public Slot(double h_slot, double h_1, double h_2,
            double b_so, double b_1, double b_2,
            double R, double r, double d_iso) : base(h_slot, h_1, h_2, b_so, b_1, b_2, R, r, d_iso)
        {
            double _rModified = (this._r > Insulation) ? this._r - Insulation : 0.001;

            SlotPreprocessor _slotInner = new SlotPreprocessor(this.Height - this.Insulation, this._h_1, this._h_2,
                this._b_so - 2.0 * Insulation, this._b_1 - 2.0 * Insulation,
                this._b_2 - 2.0 * Insulation, this._R,
                _rModified, this.Insulation);

            this._innerOutlineBase = new Point[14];
            for (int i = 2; i < 16; i++)
            {
                this._innerOutlineBase[i - 2] = _slotInner._outlineBase[i];
            }

            this.InnerOutlineRotated = new Point[14];
            for (int i = 0; i < 14; i++)
            {
                this.InnerOutlineRotated[i] = this._innerOutlineBase[i].RotateByRad(this.BasePoint, -Math.PI / 2.0) - this.BasePoint;
            }
            this.InnerArea = _slotInner.Area;
        }

        public void SetAngleByRad(double angle)
        {
            this.Outline = new Point[this._outlineBase.Length];
            for (int i = 0; i < this._outlineBase.Length; i++)
            {
                this.Outline[i] = this._outlineBase[i].RotateByRad(new Point(0.0, 0.0), angle);
            }

            this.InnerOutline = new Point[this._innerOutlineBase.Length];
            for (int i = 0; i < this._innerOutlineBase.Length; i++)
            {
                this.InnerOutline[i] = this._innerOutlineBase[i].RotateByRad(new Point(0.0, 0.0), angle);
            }
        }

        public object Clone()
        {
            return this.MemberwiseClone();
        }

    }

    public class WireArrangement
    {
        public Slot Slot { get; private set; }
        public List<List<Wire>> Windings { get; set; }
        public List<List<Wire>> Wires { get; private set; }
        public bool Valid { get; private set; } = false;
        public int NumberOfWires { get; private set; } = 0;
        public WireArrangement(Slot Slot)
        {
            this.Slot = (Slot)Slot.Clone();
        }
        private List<Wire> _getFirstRowArrangement(Point[] wireoutline, double y0, double diameter,
            double spacing_between)
        {
            double _height0 = this.Slot.Insulation;
            Wire circ0 = new Wire(new Point(0, diameter / 2.0 + y0 + _height0), diameter);
            bool res = circ0.DetectInsidePolygon(wireoutline);
            if (res == false)
            {
                for (int i = 0; i < 1000; i++)
                {
                    double _dy = diameter / 100.0;
                    circ0 = circ0.GenerateByOffset(0.0, _dy);
                    res = circ0.DetectInsidePolygon(wireoutline);
                    if (res == true) break;
                }
                if (res == false) return new List<Wire> { };
            }

            double dx = diameter + spacing_between;
            List<Wire> _posCircles = new List<Wire> { };
            Wire circ = (Wire)circ0.Clone();
            for (int i = 0; i < int.MaxValue; i++)
            {
                circ = circ.GenerateByOffset(dx, 0.0);
                res = circ.DetectInsidePolygon(wireoutline);
                if (res == true) _posCircles.Add(circ);
                else break;
            }
            List<Wire> _mirrorCircles = new List<Wire> { };
            for (int i = 0; i < _posCircles.Count; i++)
            {
                Wire _currentCircle = (Wire)_posCircles[_posCircles.Count - 1 - i].Clone();
                _currentCircle.Center = _currentCircle.Center.MirrorY();
                _mirrorCircles.Add(_currentCircle);
            }

            List<Wire> _circles = new List<Wire> { };
            foreach (Wire c in _mirrorCircles) _circles.Add(c);
            _circles.Add(circ0);
            foreach (Wire c in _posCircles) _circles.Add(c);

            Wire _circleTest = _circles[0].GenerateByOffset(-dx / 2.0, 0.0);
            res = _circleTest.DetectInsidePolygon(wireoutline);
            if (res == true)
            {
                Wire _circleLast = _circles[_circles.Count - 1].GenerateByOffset(dx / 2.0, 0.0);
                for (int j = 0; j < _circles.Count; j++)
                {
                    _circles[j] = _circles[j].GenerateByOffset(-dx / 2.0, 0.0);
                }
                _circles.Add(_circleLast);
            }
            return _circles;
        }
        private List<Wire> _getFollowingRowArrangements(Point[] wireoutline, double diameter,
            double spacing_between, int layer, List<Wire> row0)
        {
            double dx = (diameter + spacing_between) / 2.0 * (layer % 2);
            double dX = diameter + spacing_between;
            double dy = dX * Math.Sin(Math.PI / 3.0) * layer;
            if (row0[0].Center.Y + dy >= this.Slot.Height) { return new List<Wire> { }; }

            List<Wire> _row0Processed = new List<Wire> { };
            for (int i = 0; i < row0.Count; i++)
            {
                _row0Processed.Add(row0[i].GenerateByOffset(dx, dy));
            }

            List<Wire> _posCirclesCopy = new List<Wire> { };
            for (int i = 0; i < _row0Processed.Count; i++)
            {
                Wire _circleNow = _row0Processed[i];
                if (_circleNow.Center.X >= 0) _posCirclesCopy.Add(_circleNow);
            }

            Wire _circleLast = (Wire)_posCirclesCopy[_posCirclesCopy.Count - 1].Clone();
            for (int i = 0; i < int.MaxValue; i++)
            {
                Wire _circleNow = _circleLast.GenerateByOffset(dX * (i + 1), 0.0);
                if (_circleNow.DetectInsidePolygon(wireoutline) == true)
                {
                    _posCirclesCopy.Add(_circleNow);
                }
                else break;
            }

            List<Wire> _posCircles = new List<Wire> { };
            for (int i = 0; i < _posCirclesCopy.Count; i++)
            {
                Wire _circleNow = _posCirclesCopy[i];
                if (_circleNow.DetectInsidePolygon(wireoutline) == true)
                {
                    _posCircles.Add(_circleNow);
                }
                else break;
            }
            if (_posCircles.Count == 0) return new List<Wire> { };

            List<Wire> _circles = new List<Wire> { };
            for (int i = 0; i < _posCircles.Count; i++)
            {
                Wire _circleNow = _posCircles[_posCircles.Count - 1 - i];
                if (_circleNow.Center.X > 1e-3)
                {
                    Wire _circleCopy = (Wire)_circleNow.Clone();
                    _circleCopy.Center = _circleCopy.Center.MirrorY();
                    _circles.Add(_circleCopy);
                }
            }

            for (int i = 0; i < _posCircles.Count; i++)
            {
                _circles.Add(_posCircles[i]);
            }
            return _circles;
        }

        public void GetWires(double y0, double diameter, double spacing_between, int target_wires)
        {

            List<List<Wire>> _wiring = new List<List<Wire>> { };
            List<Wire> _line0 = _getFirstRowArrangement(this.Slot.InnerOutlineRotated, y0, diameter, spacing_between);
            if (_line0.Count == 0) 
            {
                this.Wires = _wiring;
                return;
            }

            for (int i = 0; i < _line0.Count; i++)
            {
                _line0[i].SetLabel("L1W" + (i + 1).ToString());
            }
            int _sum = _line0.Count;
            if (target_wires < _sum)
            {
                List<Wire> _line0Valid = new List<Wire> { };
                for (int j = 0; j < _sum; j++) _line0Valid.Add(_line0[j]);
                _wiring.Add(_line0Valid);
                this.Wires = _wiring;
                return;
            }
            _wiring.Add(_line0);
            for (int i = 1; i < int.MaxValue; i++)
            {
                int _countIndex = target_wires - _sum;
                List<Wire> _line = _getFollowingRowArrangements(this.Slot.InnerOutlineRotated, diameter,
                    spacing_between, i, _line0);
                if (_line.Count == 0) break;
                for (int k = 0; k < _line.Count; k++)
                {
                    _line[k].SetLabel("L" + (i + 1).ToString() + "W" + (k + 1).ToString());
                }
                _sum += _line.Count;

                if (target_wires < _sum)
                {
                    List<Wire> _lineValid = new List<Wire> { };
                    for (int j = 0; j < _countIndex; j++) _lineValid.Add(_line[j]);
                    if (_lineValid.Count > 0) { _wiring.Add(_lineValid); }
                    _sum = target_wires;
                    break;
                }
                else _wiring.Add(_line);
            }
            this.NumberOfWires = _sum;
            this.Wires = _wiring;

            if (target_wires == _sum) this.Valid = true;
            else this.Valid = false;
        }

        public void GetWindings(int parallel_wires, int turns, string keyword)
        {
            if (this.Valid == false)
            {
                this.Windings = new List<List<Wire>> { };
                return;
            }

            if (parallel_wires * turns != this.NumberOfWires)
            {
                this.Windings = new List<List<Wire>> { };
                return;
            }


            List<List<Wire>> _windings = new List<List<Wire>> { };
            List<Wire> _wiringFlat = new List<Wire> { };
            if (keyword == "best")
            {
                for (int i = 0; i < this.Wires.Count; i++)
                {
                    for (int j = 0; j < this.Wires[i].Count; j++)
                    {
                        _wiringFlat.Add(this.Wires[i][j]);
                    }
                }
            }
            if (keyword == "worst")
            {
                for (int i = 0; i < int.MaxValue; i++)
                {
                    for (int j = 0; j < this.Wires.Count; j++)
                    {
                        try { _wiringFlat.Add(this.Wires[j][i]); }
                        catch { };
                    }
                    if (_wiringFlat.Count == parallel_wires * turns) break;
                }
            }
            for (int i = 0; i < turns; i++)
            {
                List<Wire> _windingsOfTurn = new List<Wire>(new Wire[parallel_wires]);
                for (int j = 0; j < parallel_wires; j++)
                {
                    int index = i * parallel_wires + j;
                    _windingsOfTurn[j] = _wiringFlat[index];
                }
                _windings.Add(_windingsOfTurn);
            }

            this.Windings = _windings;

            if (keyword == "worst")
            {
                List<List<Wire>> _windingsRevised = new List<List<Wire>> { };
                for (int i = 0; i < turns; i++)
                {
                    var _windingsOfTurn = _windings[i].ToArray();
                    double[] _windingsOfTurnHeightWeighted = new double[parallel_wires];
                    int[] _indices = new int[parallel_wires];
                    for (int j = 0; j < parallel_wires; j++)
                    {
                        _windingsOfTurnHeightWeighted[j] = _windingsOfTurn[j].Center.Y
                            + _windingsOfTurn[j].Center.X * 0.0001;
                        _indices[j] = j;
                    }
                    Array.Sort(_windingsOfTurnHeightWeighted, _windingsOfTurn);
                    _windingsRevised.Add(new List<Wire>(_windingsOfTurn));
                }
                this.Windings = _windingsRevised;
            }

            _resetWindingsAngle();
            AssignCircuit();
        }

        public void AssignCircuit()
        {
            string[] _colors = new string[] { "aqua", "aquamarine", "bisque", "blue",
                "brown", "burlywood", "cadetblue", "chartreuse", "chocolate", "coral",
                "cornflowerblue", "cornsilk", "crimson", "cyan", "darkblue", "darkcyan",
                "darkgoldenrod", "darkgray", "darkgreen", "darkgrey", "darkkhaki",
                "darkmagenta", "darkolivegreen", "darkorange", "darkorchid", "darkred",
                "darksalmon", "darkseagreen", "darkslateblue", "darkslategray",
                "darkslategrey", "darkturquoise", "darkviolet", "deeppink", "deepskyblue",
                "dimgray", "dimgrey", "dodgerblue", "firebrick", "floralwhite",
                "forestgreen", "fuchsia", "gainsboro", "ghostwhite", "gold", "goldenrod",
                "gray", "green", "greenyellow", "grey", "honeydew", "hotpink",
                "indianred", "indigo", "ivory", "khaki", "lavender", "lavenderblush",
                "lawngreen", "lemonchiffon", "lightblue", "lightcoral", "lightcyan",
                "lightgoldenrodyellow", "lightgray", "lightgreen", "lightgrey",
                "lightpink", "lightsalmon", "lightseagreen", "lightskyblue",
                "lightslategray", "lightslategrey", "lightsteelblue", "lightyellow",
                "lime", "limegreen", "linen", "magenta", "maroon", "mediumaquamarine",
                "mediumblue", "mediumorchid", "mediumpurple", "mediumseagreen",
                "mediumslateblue", "mediumspringgreen", "mediumturquoise",
                "mediumvioletred", "midnightblue", "mintcream", "mistyrose", "moccasin",
                "navajowhite", "navy", "oldlace", "olive", "olivedrab", "orange",
                "orangered", "orchid", "palegoldenrod", "palegreen", "paleturquoise",
                "palevioletred", "papayawhip", "peachpuff", "peru", "pink", "plum",
                "powderblue", "purple", "rebeccapurple", "red", "rosybrown", "royalblue",
                "saddlebrown", "salmon", "sandybrown", "seagreen", "seashell", "sienna",
                "silver", "skyblue", "slateblue", "slategray", "slategrey", "snow",
                "springgreen", "steelblue", "tan", "teal", "thistle", "tomato",
                "turquoise", "violet", "wheat", "yellow", "yellowgreen" };

            for (int i = 0; i < this.Windings.Count; i++)
            {
                int _indexColor = i % _colors.Length;
                string _color = _colors[_indexColor];
                for (int j = 0; j < this.Windings[i].Count; j++)
                {
                    this.Windings[i][j].SetCircuit((j + 1).ToString());
                    this.Windings[i][j].SetColor(_color);
                }
            }
        }

        private void _resetWindingsAngle()
        {

            for (int i = 0; i < this.Windings.Count; i++)
            {
                for (int j = 0; j < this.Windings[i].Count; j++)
                {
                    Wire _circle = this.Windings[i][j];
                    _circle.Center = (_circle.Center + this.Slot.BasePoint).RotateByRad(this.Slot.BasePoint, Math.PI / 2.0);
                }
            }
        }

        public void SetAngleByRad(double angle)
        {
            for (int i = 0; i < this.Windings.Count; i++)
            {
                for (int j = 0; j < this.Windings[i].Count; j++)
                {
                    Wire _circle = this.Windings[i][j];
                    _circle.Center = _circle.Center.RotateByRad(new Point(0.0, 0.0), angle);
                }
            }
            this.Slot.SetAngleByRad(angle);
        }

        public void ReverseWindings()
        {
            for (int i = 0; i < this.Windings.Count; i++)
            {
                List<Wire> _windingsOfTurn = this.Windings[i];
                _windingsOfTurn.Reverse();
            }
            AssignCircuit();
        }

        public void ReverseWindings(int i)
        {
            List<Wire> _windingsOfTurn = this.Windings[i];
            try { _windingsOfTurn.Reverse(); }
            catch { }
            AssignCircuit();
        }
    }


    public class Point
    {
        public double X { get; private set; }
        public double Y { get; private set; }
        public Point(double x, double y)
        {
            this.X = x;
            this.Y = y;
        }

        public static Point operator +(Point pa, Point pb)
        {
            return new Point(pa.X + pb.X, pa.Y + pb.Y);
        }
        public static Point operator -(Point pa, Point pb)
        {
            return new Point(pa.X - pb.X, pa.Y - pb.Y);
        }
        public static Point operator /(Point pa, double b)
        {
            return new Point(pa.X / b, pa.Y / b);
        }
        public static double operator *(Point pa, Point pb)
        {
            return pa.X * pb.X + pa.Y * pb.Y;
        }
        public Point RotateByRad(Point pbase, double angle)
        {
            var p0 = new Point(this.X - pbase.X, this.Y - pbase.Y);
            var p1 = new Point(p0.X * Math.Cos(angle) - p0.Y * Math.Sin(angle),
                p0.Y * Math.Cos(angle) + p0.X * Math.Sin(angle));
            return p1 + pbase;
        }
        public Point RotateByDeg(Point pbase, double angle)
        {
            return RotateByRad(pbase, angle / 180.0 * Math.PI);
        }
        public Point MirrorX()
        { return new Point(this.X, -this.Y); }
        public Point MirrorY()
        { return new Point(-this.X, this.Y); }
        public double[] ConvertToArray()
        {
            return new double[] { this.X, this.Y };
        }
        public double GetDistance(Point p2)
        {
            return Math.Sqrt(Math.Pow(this.X - p2.X, 2.0) + Math.Pow(this.Y - p2.Y, 2.0));
        }
        public static double GetAngleRad(Point pbase, Point p1, Point p2)
        {
            var p1_ = p1 - pbase;
            var p2_ = p2 - pbase;
            double ang1 = Math.Atan2(p1_.Y, p1_.X);
            double ang2 = Math.Atan2(p2_.Y, p2_.X);
            return ang2 - ang1;
        }
        public static double GetAngleDeg(Point pbase, Point p1, Point p2)
        {
            return GetAngleRad(pbase, p1, p2) / Math.PI * 180.0;
        }
    }

    public class Circle
    {
        protected Point _center = new Point(0, 0);
        public Point Center
        {
            get { return _center; }
            set
            {
                _center = value;
                Left = value - new Point(_diameter / 2.0, 0);
                Right = value + new Point(_diameter / 2.0, 0);
                Top = value + new Point(0, _diameter / 2.0);
                Bottom = value - new Point(0, _diameter / 2.0);
            }
        }
        protected double _diameter = 0;
        public double Diameter
        {
            get { return _diameter; }
            set
            {
                _diameter = value;
                Left = _center - new Point(value / 2.0, 0);
                Right = _center + new Point(value / 2.0, 0);
                Top = _center + new Point(0, value / 2.0);
                Bottom = _center - new Point(0, value / 2.0);
            }
        }

        public Point Left { get; private set; }
        public Point Right { get; private set; }
        public Point Top { get; private set; }
        public Point Bottom { get; private set; }
        public String Label { get; private set; }
        public Circle(Point center, double diameter)
        {
            Center = center;
            Diameter = diameter;
        }
        public void SetLabel(string label) { Label = label; }
        public bool DetectInsidePolygon(Point[] polygon)
        {
            CrossingDetector cd = new CrossingDetector(polygon);
            double xm = 0;
            double ym = 0;
            for (int i = 0; i < polygon.Length; i++)
            {
                xm += polygon[i].X;
                ym += polygon[i].Y;
            }
            xm /= polygon.Length;
            ym /= polygon.Length;
            Point pm = new Point(xm, ym);
            for (int j = 0; j < 12; j++)
            {
                bool res = cd.PolygonCrossingSingle(Right.RotateByDeg(_center, 360.0 / 12.0 * j), pm);
                if (res == true) { return false; }
            }
            return true;
        }
    }

    public class Wire : Circle, ICloneable
    {
        public Wire(Point center, double diameter) : base(center, diameter) { }
        public string Circuit { get; private set; } = "";
        public string Color { get; private set; } = "";
        public double Loss { get; set; } = 0.0;
        public double Volume { get; set; } = 0.0;
        public void SetCircuit(string circuit)
        {
            this.Circuit = circuit;
        }
        public void SetColor(string color)
        {
            this.Color = color;
        }
        public object Clone()
        {
            return this.MemberwiseClone();
        }
        public Wire GenerateByOffset(double dX, double dY)
        {
            return new Wire(_center + new Point(dX, dY), _diameter);
        }
    }

    public class CrossingDetector
    {
        public Point[] Polygon { get; private set; }
        public CrossingDetector(Point[] polygon)
        {
            Polygon = polygon;
        }

        public static Point CrossingPoint(Point p1, Point p2, Point p3, Point p4)
        {
            double x1 = p1.X;
            double y1 = p1.Y;
            double x2 = p2.X;
            double y2 = p2.Y;
            double x3 = p3.X;
            double y3 = p3.Y;
            double x4 = p4.X;
            double y4 = p4.Y;

            double x_res, y_res;
            try
            {
                x_res = (x1 * x3 * y2 - x1 * x3 * y4 - x1 * x4 * y2 + x1 * x4 * y3 -
                    x2 * x3 * y1 + x2 * x3 * y4 + x2 * x4 * y1 - x2 * x4 * y3) /
                    (x1 * y3 - x1 * y4 - x2 * y3 + x2 * y4 - x3 * y1 + x3 * y2 + x4 * y1 -
                    x4 * y2);
                y_res = (x1 * y2 * y3 - x1 * y2 * y4 - x2 * y1 * y3 + x2 * y1 * y4 -
                    x3 * y1 * y4 + x3 * y2 * y4 + x4 * y1 * y3 - x4 * y2 * y3) /
                    (x1 * y3 - x1 * y4 - x2 * y3 + x2 * y4 - x3 * y1 + x3 * y2 + x4 * y1 -
                    x4 * y2);
            }
            catch { return null; }
            return new Point(x_res, y_res);
        }
        public static int Crossing(Point p1, Point p2, Point p3, Point p4)
        {
            Point pm = CrossingPoint(p1, p2, p3, p4);
            if (pm == null) { return 0; }
            double r12 = (p1 - pm) * (p2 - pm);
            double r34 = (p3 - pm) * (p4 - pm);

            if ((r12 <= 0) && (r34 <= 0)) { return 1; }
            else { return 0; }
        }

        public bool PolygonCrossingSingle(Point p, Point pref)
        {
            int len = Polygon.Length;
            int[] crossed = new int[len];
            Point p1, p2;
            for (int i = 0; i < len; i++)
            {
                p1 = Polygon[i];
                if (i != len - 1) { p2 = Polygon[i + 1]; }
                else { p2 = Polygon[0]; }
                var res = Crossing(p1, p2, p, pref);
                crossed[i] = res;
            }
            int sum = 0;
            foreach (int cross in crossed) { sum += cross; }
            bool ret = (sum % 2 == 1) ? true : false;
            return ret;
        }
    }
}
