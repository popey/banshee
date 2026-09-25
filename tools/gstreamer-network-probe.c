/* Exercise the snap's native HTTPS stack without the Banshee UI. */
#include <gst/gst.h>
int main(int argc, char **argv) {
    gst_init(&argc, &argv);
    if (argc != 2) return 2;
    GstElement *pipeline = gst_pipeline_new(NULL);
    GstElement *source = gst_element_factory_make("souphttpsrc", NULL);
    GstElement *sink = gst_element_factory_make("fakesink", NULL);
    if (!source || !sink) return 3;
    g_object_set(source, "location", argv[1], "num-buffers", 8, NULL);
    gst_bin_add_many(GST_BIN(pipeline), source, sink, NULL);
    gst_element_link(source, sink);
    gst_element_set_state(pipeline, GST_STATE_PLAYING);
    GstBus *bus = gst_element_get_bus(pipeline);
    GstMessage *msg = gst_bus_timed_pop_filtered(bus, 30 * GST_SECOND, GST_MESSAGE_ERROR | GST_MESSAGE_EOS);
    int result = 1;
    if (msg && GST_MESSAGE_TYPE(msg) == GST_MESSAGE_EOS) { g_print("PASS: received HTTPS media data\n"); result = 0; }
    else if (msg) { GError *err; gchar *debug; gst_message_parse_error(msg, &err, &debug); g_printerr("%s\n%s\n", err->message, debug); g_error_free(err); g_free(debug); }
    else g_printerr("Timed out\n");
    if (msg) gst_message_unref(msg);
    gst_element_set_state(pipeline, GST_STATE_NULL);
    gst_object_unref(bus); gst_object_unref(pipeline);
    return result;
}
