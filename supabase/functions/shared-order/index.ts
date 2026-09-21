import { createClient } from "npm:@supabase/supabase-js@2";

const OrderImagesBucket = "order-images";
const SignedUrlSeconds = 3600;
const UuidPattern = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;

const corsHeaders = {
  "Access-Control-Allow-Origin": "*",
  "Access-Control-Allow-Headers": "authorization, x-client-info, apikey, content-type",
  "Access-Control-Allow-Methods": "POST, OPTIONS",
};

function jsonResponse(body: unknown, status = 200): Response {
  return new Response(JSON.stringify(body), {
    status,
    headers: { ...corsHeaders, "Content-Type": "application/json" },
  });
}

Deno.serve(async (req: Request) => {
  if (req.method === "OPTIONS") {
    return new Response("ok", { headers: corsHeaders });
  }

  if (req.method !== "POST") {
    return jsonResponse({ message: "Method not allowed." }, 405);
  }

  let token: unknown;
  try {
    ({ token } = await req.json());
  } catch {
    return jsonResponse({ message: "This link is not valid." }, 400);
  }

  if (typeof token !== "string" || !UuidPattern.test(token)) {
    return jsonResponse({ message: "This link is not valid." }, 400);
  }

  const supabase = createClient(
    Deno.env.get("SUPABASE_URL")!,
    Deno.env.get("SUPABASE_SERVICE_ROLE_KEY")!,
  );

  const { data: order, error } = await supabase.rpc("get_shared_order", { p_token: token });
  if (error) {
    console.error(error.message);
    return jsonResponse({ message: "This order could not be loaded." }, 500);
  }
  if (!order) {
    return jsonResponse({ message: "This order could not be found." }, 404);
  }

  await attachSignedUrls(supabase, order);
  return jsonResponse(order);
});

async function createSignedUrl(
  supabase: ReturnType<typeof createClient>,
  path: string | null | undefined,
): Promise<string | null> {
  if (!path) return null;

  const { data, error } = await supabase.storage
    .from(OrderImagesBucket)
    .createSignedUrl(path, SignedUrlSeconds);
  if (error) {
    console.error(error.message);
    return null;
  }
  return data.signedUrl;
}

async function attachSignedUrls(
  supabase: ReturnType<typeof createClient>,
  order: any,
): Promise<void> {
  if (order.upload) {
    order.upload.image_url = await createSignedUrl(supabase, order.upload.image_path);
    delete order.upload.image_path;
  }

  const elements = order.custom?.design_data?.elements;
  if (!Array.isArray(elements)) return;

  for (const element of elements) {
    if (element.type === "image") {
      element.dataUrl = await createSignedUrl(supabase, element.path);
    }
    delete element.path;
  }
}
