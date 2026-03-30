using Prometheus;

namespace OrderService.Metrics;

public static class OrderMetrics
{
    public static readonly Counter OrdersCreated =
        Prometheus.Metrics.CreateCounter(
            "orders_created_total",
            "Number of orders created");

    public static readonly Counter OrdersValidated =
        Prometheus.Metrics.CreateCounter(
            "orders_validated_total",
            "Number of orders validated by warehouse");

    public static readonly Counter OrdersRejected =
        Prometheus.Metrics.CreateCounter(
            "orders_rejected_total",
            "Number of orders rejected by warehouse");
}
