import React, { Component } from 'react';
import { View, TouchableOpacity, StyleSheet } from 'react-native';
import { RNCamera } from 'react-native-camera';
import CustomVisionService from '../services/customVisionServiceHelper';

class CameraScreen extends React.Component {
    constructor(props) {
        super(props);
        this.customVisionService = new CustomVisionService();
        this.state = {
            isCameraVisible: false
        }
    }

    componentDidMount() {
        const { navigation } = this.props;
        let specData = navigation.getParam('specData', 'unknown');

        this.setState({
            specData: specData
        });
    }

    render() {
        const { mainContainer, cameraPreview, cameraControlsContainer, takePictureButton,
                horizontalLineContainer, verticalLineContainer, horizontalLineView, verticalLineView,
                horizontalLine, verticalLine } = this.styles;

        let horizontalLines = 
            <View style={horizontalLineContainer}>
                <View style={horizontalLineView}>
                    <View style={horizontalLine} />
                </View>
                <View style={horizontalLineView}>
                    <View style={horizontalLine} />
                </View>
                <View style={horizontalLineView} />
            </View>;

        let verticalLines = 
            <View style={verticalLineContainer}>
                <View style={verticalLineView}>
                    <View style={verticalLine} />
                </View>
                <View style={verticalLineView}>
                    <View style={verticalLine} />
                </View>
                <View style={verticalLineView} />
            </View>;

        let cameraControls =
            <View style={cameraControlsContainer}>
                <TouchableOpacity onPress={this.takePicture.bind(this)} style={takePictureButton} />                       
            </View>

        return (
            <View style={mainContainer}>
                <RNCamera 
                    ref={ref => { this.camera = ref; }} 
                    style={cameraPreview}
                    flashMode={RNCamera.Constants.FlashMode.auto}
                    androidCameraPermissionOptions={{
                        title: 'Permission to use camera',
                        message: 'We need your permission to use your camera',
                        buttonPositive: 'Ok',
                        buttonNegative: 'Cancel',
                    }}
                    androidRecordAudioPermissionOptions={{
                        title: 'Permission to use audio recording',
                        message: 'We need your permission to use your audio',
                        buttonPositive: 'Ok',
                        buttonNegative: 'Cancel',
                    }}>

                    {horizontalLines}
                    {verticalLines}

                    {cameraControls}
                </RNCamera>
            </View>
        );
    }

    takePicture = async() => {
        if (this.camera) {
            const { navigate } = this.props.navigation;
            
            const options = { quality: 0.5, base64: true };
            const data = await this.camera.takePictureAsync(options);

            console.log(data.uri);
            this.setState({ isCameraVisible: false });

            navigate('Review', {image: data, selectedSpec: this.state.specData, fromCamera: true });
        }
    }

    styles = StyleSheet.create({
        mainContainer: {
            flex: 1, 
            flexDirection: 'column', 
            backgroundColor: 'transparent'
        },
        cameraPreview: {
            flex: 1, 
            justifyContent: 'flex-end', 
            alignItems: 'center'
        },
        cameraControlsContainer: {
            flexDirection: 'row', 
            justifyContent: 'center', 
            opacity: 0.6
        },
        takePictureButton: {
            flex: 0,
            backgroundColor: '#fff',
            borderRadius: 25,
            alignSelf: 'center',
            margin: 20,
            width: 50,
            height: 50
        },
        horizontalLineContainer: {
            position: 'absolute', 
            width: '100%', 
            height: '100%'
        },
        verticalLineContainer: {
            position: 'absolute', 
            flexDirection: 'row', 
            width: '100%', 
            height: '100%'
        },
        horizontalLineView: {
            flex: 1, 
            justifyContent: 'flex-end'
        },
        verticalLineView: {
            flex: 1, 
            flexDirection: 'row', 
            justifyContent: 'flex-end'
        },
        horizontalLine: {
            height: 1, 
            backgroundColor: 'white', 
            opacity: 0.6
        },
        verticalLine: {
            width: 1, 
            backgroundColor: 'white', 
            opacity: 0.6
        }
    })
}

export { CameraScreen };