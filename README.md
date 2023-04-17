# myNOC.EntityFramework.Query

## Overview

This *Query Pattern* is something I got from listening to a [.NET Rocks](https://www.dotnetrocks.com/) episode #1494 
[Developer Tips and Design Patterns with Steve Smith](https://www.dotnetrocks.com/details/1494).  [Steve Smith](https://ardalis.com/) talks about the 
*Specification Pattern* at 49:50 in the podcast, and this is what inspired this.

What Steve describes is a problem we saw in our projects.  Developers would keep adding more methods to the repository or keep adding JOINs to a result 
because they need this one other column added to an already returning list.  This will start to overly complicate the original list.  For example, you 
already have a screen that shows the list of users, and if they are active or not.  Now, some other screen needs the list of users and wants the security 
role or permissions that user has also displayed.  What happens often is someone just goes and modifies that original user list and adds this other data.  
So, where you needed a simple quick list of users is now doing way more than it needs and could potentially slow down the query.

This pattern also gives you the ability to easily test your queries.  As Steve points out in the podcast what might work fine in compile time, 
or even within a unit test using mocked lists as your data, could give you a runtime error when running against EntityFramework.  

You can checkout the code [here](./src/myNOC.EntityFramework.Query/).  I do have a simple sample project [here](./sample/QuerySample/), 
and I am working on adding unit tests.
