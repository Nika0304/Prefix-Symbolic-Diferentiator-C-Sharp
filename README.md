# 🧠 Symbolic Prefix Differentiator (C#)

A robust console application designed to perform **symbolic differentiation** on mathematical expressions. This project utilizes **Prefix Notation** and recursive algorithms to calculate derivatives based on standard calculus rules.

## 🚀 Key Features

* **Recursive Differentiation Engine:** Implements the Chain Rule, Product Rule, and Quotient Rule by traversing nested expression arrays.
* **Symbolic Simplification:** Features a dedicated post-processing engine to clean mathematical outputs (e.g., eliminating `x * 0`, `u + 0`, or `u ^ 1`).
* **Comprehensive Function Support:**
    * **Arithmetic:** `+`, `-`, `*`, `/`, `^` (power), `sqrt`.
    * **Transcendental:** `sin`, `cos`, `tg`, `ctg`, `ln`, `log` (with custom base).
    * **Inverse Trigonometric:** `arcsin`, `arccos`, `arctg`, `arcctg`.
* **Dynamic Variable Selection:** Allows the user to specify the differentiation variable (e.g., `x`, `y`, `z`) at runtime.

  
### 📋 Supported Input Formats
The parser and derivation engine support two types of prefix structures:
1. **Binary Expressions:** `["operator", "operand1", "operand2"]` 
   - *Example:* `["+", "x", "1"]` -> $x + 1$
2. **Unary Expressions:** `["operator", "operand1"]`
   - *Example:* `["sin", "x"]` -> $sin(x)$
   - *Example:* `["-", "x"]` -> $-x$
## 🛠️ Technical Implementation

The application processes mathematical expressions as recursive objects (`List<object>`). The workflow consists of three main stages:

1.  **Parsing:** The input string is tokenized and converted into a hierarchical list structure.
2.  **Raw Derivation:** The `DeriveRaw` method applies the derivative table recursively.
3.  **Simplification:** The `Simplify` method performs a bottom-up pass to ensure the final expression is optimized and human-readable.



## 📋 Usage Examples

When the differentiation variable is set to `x`:

| Input (Prefix Format) | Standard Infix Notation | Result (Simplified Derivative) |
| :--- | :--- | :--- |
| `["sin", "x"]` | $sin(x)$ | `["cos", "x"]` |
| `["^", "x", 3]` | $x^3$ | `["*", "3", ["^", "x", "2"]]` |
| `["sin", ["cos", "x"]]` | $sin(cos(x))$ | `["*", ["cos", ["cos", "x"]], ["-", ["sin", "x"]]]` |
| `["ln", "x"]` | $ln(x)$ | `["/", "1", "x"]` |

## 💻 Installation & Execution

1.  Ensure you have the **.NET SDK** installed.
2.  Clone the repository or save `Program.cs`.
3.  Run the following commands in your terminal:

```bash
dotnet build
dotnet run
