1. Run `docker-compose up`.
2. Send some requests via `test.rest` with trace-context and without.
3. Open [Zipkin](http://localhost:9411/zipkin/traces/0af7651916cd43dd8448eb211c80319c)
4. Open [Jaeger](http://localhost:16686/trace/0af7651916cd43dd8448eb211c80319c)
5. Open [Grafana/Tempo](http://localhost:3000/explore?orgId=1&left=%5B%22now-1h%22,%22now%22,%22Tempo%22,%7B%22queryType%22:%22traceId%22,%22query%22:%220af7651916cd43dd8448eb211c80319c%22%7D%5D)  
6. Open [Prometheus](http://localhost:9090/graph?g0.expr=process_runtime_dotnet_gc_allocations_size_bytes&g0.tab=0&g0.stacked=0&g0.show_exemplars=0&g0.range_input=1h&g1.expr=process_runtime_dotnet_monitor_lock_contention_count&g1.tab=0&g1.stacked=0&g1.show_exemplars=0&g1.range_input=1h)
7. Open [Grafana](http://localhost:3000/explore?orgId=1&left=%5B%22now-1h%22,%22now%22,%22Prometheus%22,%7B%22exemplar%22:true,%22expr%22:%22rate(process_runtime_dotnet_exceptions_count%7Binstance%3D%5C%22rawvsotelpocapi:7130%5C%22%7D%5B$__interval%5D)%22,%22hide%22:false%7D,%7B%22exemplar%22:true,%22expr%22:%22process_runtime_dotnet_gc_allocations_size_bytes%7Binstance%3D%5C%22rawvsotelpocapi:7130%5C%22%7D%22%7D%5D)