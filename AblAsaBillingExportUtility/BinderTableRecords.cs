using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace AblAsaBillingExportUtility
{
    public class BinderTableRecords
    {
        private int max_spine_width;
        private int max_board_height;
        private int max_board_width;

        public int MaxSpineWidth
        {
            get { return max_spine_width; }
            set { max_spine_width = value; }
        }

        public int MaxBoardHeight
        {
            get { return max_board_height; }
            set { max_board_height = value; }
        }

        public int MaxBoardWidth
        {
            get { return max_board_width; }
            set { max_board_width = value; }
        }
    }
}