open System.Threading.Tasks
open Npgsql.FSharp
open Npgsql

let connectionString : string =
    Sql.host "localhost"
    |> Sql.database "test_db"
    |> Sql.username "test_user"
    |> Sql.password "test123"
    |> Sql.port 5432
    |> Sql.formatConnectionString

let connpool : string = connectionString + ";Maximum Pool Size=15"
printfn "Data Source from %s" connpool
// construct the Data Source
use dataSource = NpgsqlDataSource.Create(connpool)
    

let checkConnectionPool (dataSource: NpgsqlDataSource) : Task<int list> =
    dataSource
    |> Sql.fromDataSource
    |> Sql.query "select count(*) as conn_num from pg_stat_activity where usename='test_user';"
    |> Sql.executeAsync (fun read ->
        read.int "conn_num")


type Distributor = { Id: int; Name: string; }
let getDistributors (dataSource: NpgsqlDataSource) (myid: int): Async<Distributor list> = async {
    let! res =
        dataSource
        |> Sql.fromDataSource
        |> Sql.query "SELECT * FROM distributors WHERE did = @id"
        |> Sql.parameters [ "@id", Sql.int myid ]
        |> Sql.executeAsync (fun read ->
            {
                Id = read.int "did"
                Name = read.text "name"

            })
        |> Async.AwaitTask
    let! connNum = checkConnectionPool dataSource |> Async.AwaitTask
    connNum
    |> List.head
    |> printfn "connections: %d" 
    return res
    }


async {
    printfn "start"
    let tasks = 
        [|1..30|]
        |> Array.map (getDistributors dataSource)
        
    printfn "tasks ready"
    let tasks = 
        tasks
        |> Async.Parallel
    printfn "tasks parallel"
    
    let! tasks = tasks
    tasks
    |> Array.iter(
        List.iter (fun d -> 
            printfn "name : %s id %d" d.Name d.Id)
    )
    
    printfn "end"
} 
|> Async.RunSynchronously
  