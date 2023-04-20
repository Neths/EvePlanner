import {createSlice} from "@reduxjs/toolkit";


const initialState = {
  modal: false,
  title: '',
  content: [],
  actionButtonText: '',
  actionButtonOnClick: () => {}
}

export const modalSlice = createSlice({
  name: 'modal',
  initialState,
  reducers: {
    showModal: (state, action) => {
      state.modal = true
      state.title = action.payload.title
      state.content = action.payload.content
      state.actionButtonText = action.payload.actionButtonText
      state.actionButtonOnClick = action.payload.actionButtonOnClick
      return state
    },
    hideModal: (state, action) => {
      state.modal = false
      return state
    }
  }
})

export const { showModal, hideModal } = modalSlice.actions

export default modalSlice.reducer