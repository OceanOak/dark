import React from "react";
import Prompt from './Prompt';
import UserChat from "./UserChat";


const Chat = () => {

  return (
    <div className=" h-auto relative w-full flex flex-col items-center justify-center">
      <UserChat/>
    </div>
  );
};

export default Chat;