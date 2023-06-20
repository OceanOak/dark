<script setup lang="ts">
import { defineProps, ref } from 'vue'
import type { Task, Action } from '@/types'

const props = defineProps<{
  tasks: Task[]
  actions: Action[]
  fnList: string[]
}>()
let fnBody = ref(props.fnList[0])
let showTextArea = ref(false)
function displayTextArea(index: number) {
  showTextArea.value = true
  getFnBody()
  console.log('fnList', props.fnList)
  fnBody.value = props.fnList[index]
}

async function getFnBody() {
  try {
    console.log('trying ...')
    const result = await window.darklang.handleEvent({ GetFnBody: [] })
    console.log('result', result)
  } catch (error) {
    console.error(error)
  }
}

async function save() {
  try {
    console.log('trying ...')
    console.log('fnBody', fnBody.value)
    const result = await window.darklang.handleEvent({ SaveFnList: [fnBody.value] })
    console.log('result', result)
  } catch (error) {
    console.error(error)
  }
}
</script>

<template>
  <div class="min-h-screen bg-[#151515] text-white">
    <h1 class="p-2 mt-2 font-semibold">Tasks And Actions</h1>
    <div class="p-2 m-2 rounded bg-[#3a3a3a]">
      <h2 class="font-semibold">Tasks</h2>
      <ul class="text-[#9ea4ac] text-sm">
        <li v-for="(task, index) in tasks" :key="index">
          {{ task.description }}
        </li>
      </ul>
    </div>
    <div class="p-2 m-2 rounded bg-[#3a3a3a]">
      <h2 class="font-semibold">Actions</h2>
      <ul class="text-[#9ea4ac] text-sm">
        <li v-for="(action, index) in actions" :key="index">
          {{ action.description }}
        </li>
      </ul>
    </div>
    <div class="p-2 m-2 rounded bg-[#3a3a3a]">
      <h2 class="font-semibold">Functions</h2>
      <ul class="text-[#9ea4ac] text-sm">
        <li
          class="cursor-pointer"
          @click="displayTextArea(index)"
          v-for="(fn, index) in fnList"
          :key="index"
        >
          {{ fn }}
        </li>
      </ul>
    </div>
    <div v-if="showTextArea" class="p-2 m-2 rounded bg-[#3a3a3a]">
      <h2 class="font-semibold">Function</h2>
      <textarea
        v-model="fnBody"
        class="text-[#9ea4ac] text-sm"
        placeholder="Enter function here"
      ></textarea>
      <button @click="save" class="bg-[#3a3a3a] text-[#9ea4ac] text-sm">save</button>
    </div>
  </div>
</template>
