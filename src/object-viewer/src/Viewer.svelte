<script lang="ts">
    //   import { getRenderType } from "meta";
  
    export let value: unknown;
  
    $: info = getRenderType(value);
  
    type ObjectType =
      | "string"
      | "number"
      | "bigint"
      | "boolean"
      | "symbol"
      | "undefined"
      | "object"
      | "function";
  
    type RenderType = "simple-value" | "object" | "array";
  
    function getKeyValues(value: object) {
      if (!value) {
        return [];
      }
      return Object.entries(value).map(([key, value]) => ({ key, value }));
    }
  
    function getCommonKeys(value: unknown[]): string[] {
      if (!value) {
        return [];
      }
      const keys = new Set<string>([]);
      for (const row of value) {
        for (const key of Object.keys(row)) {
          keys.add(key);
        }
      }
      return Array.from(keys);
    }
  
    function getRenderType(
      value: any
    ): { type: RenderType; value: any; keys?: string[] } {
      const type = typeof value;
      switch (type) {
        case "string":
        case "number":
        case "bigint":
        case "boolean":
        case "symbol":
        case "undefined":
          return { type: "simple-value", value: value.toString() };
        case "object":
          if (value === null) return { type: "simple-value", value: "null" };
          if (Array.isArray(value)) {
            const keys = getCommonKeys(value);
            return { type: "array", value, keys };
          }
          return { type: "object", value: value };
        default:
          throw new Error("Unsupported type");
      }
    }
  </script>
  
  <style>
    table.grid {
      border-collapse: collapse;
    }
  
    table.grid th,
    td {
      border: 1px solid black;
      padding: 0.5ex;
    }
  </style>
  
  <!-- <div>{info.type}</div> -->
  {#if info.type === 'simple-value'}
    <div>{info.value}</div>
  {:else if info.type === 'object'}
    <table class="grid">
      {#each getKeyValues(info.value) as { key, value }, i}
        <tr>
          <th>{key}</th>
          <td>
            <svelte:self {value} />
          </td>
        </tr>
      {/each}
    </table>
  {:else if info.type === 'array'}
    <table class="grid">
      {#if info.keys}
        <thead>
          <tr>
            {#each info.keys as key}
              <th>{key}</th>
            {/each}
          </tr>
        </thead>
      {/if}
  
      <tbody>
        {#if info.keys}
          {#each info.value as row}
            <tr>
              {#each info.keys as key}
                <td>
                  <svelte:self value={row[key]} />
                </td>
              {/each}
            </tr>
          {/each}
        {:else}
          {#each info.value as row}
            <tr>
              <td>
                <svelte:self value={row} />
              </td>
            </tr>
          {/each}
        {/if}
      </tbody>
    </table>
  {:else}
    <div>Unknown</div>
  {/if}