using System;
using System.ComponentModel;
using System.Collections.Generic;

namespace PlayingWithGeminiAI;

internal static class ToolsThatCanBeInvokedByLLMs
{
    [Description("Retrieve the proper name of the user you are talking to")]
    public static string GetMyName()
    {
        Console.WriteLine(" ------> Calling GetMyName function");
        return "Carlos Estefano Garcia";
    }

    [Description("Retrieve the age of the user you are talking to")]
    public static string GetMyAge()
    {
        Console.WriteLine(" ------> Calling GetMyAge function");
        return "42";
    }

    [Description("Order to be Reviewed by a Manager")]
    public sealed record OrderRequestedToBeReviewed(string OrderId, string Status, string EstimatedDeliveryDate, decimal TotalAmount, string[] ItemsPresentInOrder);

    [Description("List of Valid Order Statuses")]
    public enum OrderStatus
    {
        Pending,
        Shipped,
        Delivered,
        Cancelled,
        NotDetermined
    }

    [Description("Order to Print")]
    public sealed record OrderRequestedToBePrinted(string OrderId, OrderStatus Status, DateTime EstimatedDeliveryDate, decimal TotalAmount, short NumberOfItemsPresentInOrder);

    [Description("Order to Cancel")]
    public sealed record OrderRequestedToBeCancelled(string OrderId, OrderStatus Status, short NumberOfItemsPresentInOrder);

    [Description("Retrieve information about an Order")]
    public static string GetOrderInformation([Description("Number of the Order you want to get information about")] string orderNumber)
    {
        Console.WriteLine(" ------> Calling GetOrderInformation function");

        Dictionary<string, string> orderDetails = new() {
            { "XYZ123", """
                # Order Information:
                - **Order Number**: XYZ123
                - **Status**: Shipped
                - **Estimated Delivery**: 2025-08-27
                - **Items**:
                  - Item 1: Widget A
                  - Item 2: Widget B
                - **Total Amount**: $99.99
            """ },

            { "ABC00987", """
                # Order Information:
                - **Order Number**: ABC00987
                - **Status**: Shipped
                - **Estimated Delivery**: 2025-09-15
                - **Items**:
                  - Item 1: Gadget A
                  - Item 2: Widget B
                  - Item 2: Widget C
                - **Total Amount**: $45.30
            """ },
        };

        if (orderDetails.ContainsKey(orderNumber))
            return orderDetails[orderNumber];

        return $"ERROR: Order with number '{orderNumber}' **was not found**.";
    }

    [Description("Send an Order to be Reviewed by a Manager")]
    public static string SendOrderToBeReviewedByManager([Description("Information about Order which is requested to be Reviewed")] OrderRequestedToBeReviewed order)
    {
        Console.WriteLine(" ------> Calling SendOrderToBeReviewedByManager function");
        return "OK";
    }

    [Description("Send an Order to be Reviewed by a Manager")]
    public static string PrintOrder([Description("Information about Order which is requested to be Printed")] OrderRequestedToBePrinted orderToPrint)
    {
        Console.WriteLine(" ------> Calling PrintOrder function");
        return "OK";
    }

    [Description("Log a reason why a specific step in the workflow could not be performed")]
    public static string LogReasonWhyStepCannotBePerformed([Description("Information about Order which is requested to be Printed")] string reason)
    {
        Console.WriteLine(" ------> Calling LogReasonWhyStepCannotBePerformed function: " + reason);
        return "OK";
    }

    [Description("Cancel an Order")]
    public static string CancelOrder([Description("Order information to be Cancelled")] OrderRequestedToBeCancelled orderToCancel)
    {
        Console.WriteLine(" ------> Calling CancelOrder function: ");
        return TryToCancelOrder(orderToCancel);
    }

    private static string TryToCancelOrder(OrderRequestedToBeCancelled orderToCancel)
    {
        if (orderToCancel.NumberOfItemsPresentInOrder > 0)
            return $"ERROR: Order cannot be cancelled because of it has {orderToCancel.NumberOfItemsPresentInOrder} items, which have been prepped already to be sent";

        return "Order was cancelled successfully";
    }

}
