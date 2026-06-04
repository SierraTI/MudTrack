import sqlite3
import sys
import os

db_path = os.path.join("MudTrack", "projectReport", "projectReport.db")

if not os.path.exists(db_path):
    print(f"Error: Database file not found at {db_path}")
    sys.exit(1)

def run_query(query):
    try:
        conn = sqlite3.connect(db_path)
        cursor = conn.cursor()
        cursor.execute(query)
        
        # If it is a SELECT query, print rows
        if cursor.description:
            columns = [col[0] for col in cursor.description]
            rows = cursor.fetchall()
            
            if not rows:
                print("No rows returned.")
                conn.close()
                return

            # Print columns
            col_widths = [len(col) for col in columns]
            for row in rows:
                for idx, val in enumerate(row):
                    col_widths[idx] = max(col_widths[idx], len(str(val)))
                    
            format_str = " | ".join([f"{{:<{w}}}" for w in col_widths])
            print("-" * (sum(col_widths) + len(columns) * 3 - 3))
            print(format_str.format(*columns))
            print("-" * (sum(col_widths) + len(columns) * 3 - 3))
            for row in rows:
                print(format_str.format(*[str(val) if val is not None else "NULL" for val in row]))
            print("-" * (sum(col_widths) + len(columns) * 3 - 3))
            print(f"({len(rows)} rows returned)")
        else:
            conn.commit()
            print(f"Query executed successfully. Rows affected: {cursor.rowcount}")
            
        conn.close()
    except Exception as e:
        print(f"Error executing query: {e}")

if len(sys.argv) > 1:
    query = " ".join(sys.argv[1:])
    run_query(query)
else:
    print("SQLite Database Query CLI Tool")
    print(f"Connected to: {db_path}")
    print("Type your SQL query and press Enter. Type 'exit' to quit.\n")
    while True:
        try:
            query = input("SQL> ").strip()
            if not query:
                continue
            if query.lower() in ("exit", "quit"):
                break
            run_query(query)
            print()
        except KeyboardInterrupt:
            print("\nExiting...")
            break
        except EOFError:
            break
