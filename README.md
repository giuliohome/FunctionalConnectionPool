# FunConnPool



Create a [postgres db in docker](https://www.commandprompt.com/education/how-to-create-a-postgresql-database-in-docker/)

```sh
docker run --name my-postgres --env POSTGRES_PASSWORD=admin --volume postgres-volume:/var/lib/postgresql/data --publish 5432:5432 --detach postgres
```

Connect to it

```
docker exec -it my-postgres bash
```

and from the container

```
psql -h localhost -U postgres
```

Finally the data structure

```
create database test_db;
\l
\c test_db
CREATE TABLE distributors (
     did    integer PRIMARY KEY DEFAULT nextval('serial'),
     name   varchar(40) NOT NULL CHECK (name <> '')
);
```
and user
```
create user test_user  with encrypted password 'test123';
 grant all privileges on database test_db to test_user;
GRANT ALL ON SCHEMA public TO test_user;
 \q
```
Test the user `psql -h localhost test_user -d test_db` by creating a table

```
CREATE TABLE distributors (
     did    integer PRIMARY KEY,
     name   varchar(40)
);
insert into distributors(did,name) values (1,'Giulio');
insert into distributors(did,name) values (3,'Eva');
```


`exit` from the container, finally `dotnet run`


## Use a data source

In this branch I have introduced a few commits:

- back to vanilla sequential
- modern DataSource introduced
- going parallel with max pool size contrained