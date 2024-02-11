namespace ByBItBots.DTOs.Menus
{
    public abstract class MenuModel
    {
        public string Title { get; protected set; }
        public string ColumnEdge { get; protected set; }
        public int MarginColumns { get; protected set; }
        public char RowBody { get; protected set; }
        public string HeaderEdges { get; protected set; }
        public int RowLength { get; protected set; }
        public int RowBodyLength => RowLength - ColumnEdge.Length * 2;
        public List<string> Options { get; protected set; }
    }
}
