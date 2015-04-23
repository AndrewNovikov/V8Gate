using System;
using System.Collections.Generic;

namespace V8Gate {
  public class TransactionIdStack {
    private readonly Stack<Guid> _list = new Stack<Guid>();

    /// <summary>
    /// Do nothing if it's nested transaction.
    /// </summary>
    public bool IsNested {
      get {
        return _list.Count > 1;
      }
    }

    /// <summary>
    /// Returns the object at the top of the TransactionIdStack without removing it
    /// </summary>
    /// <returns></returns>
    public Guid Peek() {
      return _list.Count == 0 ? Guid.Empty : _list.Peek();
    }

    /// <summary>
    /// Inserts the object at the top of the TransactionsIdStack
    /// </summary>
    /// <param name="id"></param>
    public void Push(Guid id) {
      _list.Push(id);
    }

    /// <summary>
    /// Removes and returns the object at the top of the TransactionsIdStack
    /// </summary>
    /// <returns></returns>
    public Guid Pop() {
      return _list.Count == 0 ? Guid.Empty : _list.Pop();
    }



  }
}
