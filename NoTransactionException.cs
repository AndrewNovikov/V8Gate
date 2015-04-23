using System;

namespace V8Gate {
  public class NoTransactionException: Exception {

    public NoTransactionException(string message): base(message) {
    }
  }
}
