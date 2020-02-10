import React, { Component } from 'react';
import { View, Platform, TouchableOpacity, StyleSheet } from 'react-native';
import { RNCamera } from 'react-native-camera';
import CustomVisionService from '../services/customVisionServiceHelper';
import { ZoomView } from '../components/uikit';

const MAX_ZOOM = 7; // iOS only
const ZOOM_F = Platform.OS === 'ios' ? 0.007 : 0.08;

class CameraScreen extends React.Component {
    constructor(props) {
        super(props);
        this.customVisionService = new CustomVisionService();
        this.state = {
            zoom: 0.0,
            isCameraVisible: false
        }
    }

    componentDidMount() {
        const { navigation } = this.props;
        let specData = navigation.getParam('specData', 'unknown');

        this.setState({
            zoom: 0.0,
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
                    zoom={this.state.zoom}
                    maxZoom={MAX_ZOOM}
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

                    <ZoomView
                        onPinchEnd={this.onPinchEnd}
                        onPinchStart={this.onPinchStart}
                        onPinchProgress={this.onPinchProgress}>

                        {horizontalLines}
                        {verticalLines}

                        {cameraControls}

                    </ZoomView>

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

    onPinchStart = () => {
        this._prevPinch = 1;
    }
    
     onPinchEnd = () => {
        this._prevPinch = 1;
     }
    
    onPinchProgress = (p) => {
        let p2 = p - this._prevPinch;
        if (p2 > 0 && p2 > ZOOM_F) {
          this._prevPinch = p;
          this.setState({zoom: Math.min(this.state.zoom + ZOOM_F, 1)}, () => { });
        } else if (p2 < 0 && p2 < -ZOOM_F) {
          this._prevPinch = p;
          this.setState({zoom: Math.max(this.state.zoom - ZOOM_F, 0)}, () => { });
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
            position: 'absolute',
            bottom: 0,
            alignSelf: 'center',
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