using System;
using System.Collections.Generic;
using System.Text;

namespace ISOParser {
    /// <summary>
    /// Abstract class to represent a file/directory node 
    /// </summary>
    public class ISONode {
        #region Public Properties

        /// <summary>
        /// The record this node was created from.
        /// </summary>
        public ISONodeRecord FirstRecord;

        /// <summary>
        /// The sector offset of the file/directory data
        /// </summary>
        public long Offset;
        /// <summary>
        /// The byte length of the file/directory data.
        /// </summary>
        public long Length;

        #endregion

        #region Construction

        /// <summary>
        /// Constructor.
        /// TODO: Make this constructor protected???
        /// </summary>
        /// <param name="record">The ISONodeRecord to construct from.</param>
        public ISONode( ISONodeRecord record ) {
            this.FirstRecord = record;
            this.Offset = record.OffsetOfData;
            this.Length = record.LengthOfData;
        }

        #endregion
    }
}
