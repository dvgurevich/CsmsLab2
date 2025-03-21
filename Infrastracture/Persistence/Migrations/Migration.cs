using FluentMigrator;

[Migration(2025031801)]
public class CreateOrderManagementTables : Migration
{
    public override void Up()
    {
        Execute.Sql(@" 
            CREATE TABLE products (
                product_id BIGINT PRIMARY KEY GENERATED ALWAYS AS IDENTITY,
                product_name TEXT NOT NULL,
                product_price MONEY NOT NULL
            );
            
            CREATE TYPE order_state AS ENUM ('created', 'processing', 'completed', 'cancelled');
            
            CREATE TABLE orders (
                order_id BIGINT PRIMARY KEY GENERATED ALWAYS AS IDENTITY,
                order_state order_state NOT NULL,
                order_created_at TIMESTAMP WITH TIME ZONE NOT NULL,
                order_created_by TEXT NOT NULL
            );
            
            CREATE TABLE order_items (
                order_item_id BIGINT PRIMARY KEY GENERATED ALWAYS AS IDENTITY,
                order_id BIGINT NOT NULL REFERENCES orders(order_id),
                product_id BIGINT NOT NULL REFERENCES products(product_id),
                order_item_quantity INT NOT NULL,
                order_item_deleted BOOLEAN NOT NULL
            );
            
            CREATE TYPE order_history_item_kind AS ENUM ('created', 'item_added', 'item_removed', 'state_changed');
            
            CREATE TABLE order_history (
                order_history_item_id BIGINT PRIMARY KEY GENERATED ALWAYS AS IDENTITY,
                order_id BIGINT NOT NULL REFERENCES orders(order_id),
                order_history_item_created_at TIMESTAMP WITH TIME ZONE NOT NULL,
                order_history_item_kind order_history_item_kind NOT NULL,
                order_history_item_payload JSONB NOT NULL
            );
        ");
    }

    public override void Down()
    {
        Execute.Sql(@" 
            DROP TABLE order_history;
            DROP TYPE order_history_item_kind;
            DROP TABLE order_items;
            DROP TABLE orders;
            DROP TYPE order_state;
            DROP TABLE products;
        ");
    }
}